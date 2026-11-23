using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SVappsLAB.iRacingTelemetrySDK;
using PitWallConnector.Models;
using PitWallConnector.Services;

public class Program
{
    private static readonly JsonSerializerOptions SessionJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("          PIT WALL CONNECTOR            ");
        Console.WriteLine("========================================");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger("PitWallConnector");
        var currentDrivers = new Dictionary<int, DriverTelemetry>();
        var driversLock = new object();
        JsonElement? sessionInfo = null;
        bool connected = false;

        var paceTracker = new PaceTracker();
        var fuelTracker = new FuelTracker();
        var pitStopTracker = new PitStopTracker();
        var relativeTracker = new RelativeTracker();
        var liveOrderTracker = new LiveOrderTracker();
        var qualifyingTracker = new QualifyingTracker();
        string currentSessionType = "";
        int lastSessionNumber = int.MinValue;
        await using var publisher = new SnapshotPublisher();

        long lastSnapshotTimestamp = 0;
        long lastConsoleTimestamp = 0;

        try
        {
            var clientOptions = new ClientOptions
            {
                DeliveryMode = TelemetryDeliveryMode.Synchronous
            };

            await using var client = TelemetryClient<DynamicTelemetryData>.Create(
                logger,
                null,
                clientOptions);

            Console.WriteLine("Telemetry client initialized.");
            Console.WriteLine("Waiting for iRacing...");
            Console.WriteLine("Snapshot JSON: Data/latest-snapshot.json");
            Console.WriteLine("Optional API: set PITWALL_API_URL and PITWALL_API_TOKEN environment variables.");
            Console.WriteLine();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            var handlers = new TelemetryHandlers<DynamicTelemetryData>
            {
                OnConnectStateChanged = state =>
                {
                    connected = string.Equals(state.ToString(), "Connected", StringComparison.OrdinalIgnoreCase);
                    Console.WriteLine($"iRacing connection: {state}");
                    return Task.CompletedTask;
                },

                OnSessionInfoUpdate = session =>
                {
                    try
                    {
                        JsonElement parsed = JsonSerializer.SerializeToElement(session, SessionJsonOptions);
                        JsonElement filteredSessionInfo = BuildFilteredSessionInfo(parsed);
                        lock (driversLock)
                        {
                            sessionInfo = filteredSessionInfo;
                            qualifyingTracker.ReadOfficialResults(parsed);
                            currentDrivers.Clear();

                            var drivers = session.DriverInfo?.Drivers;
                            if (drivers != null)
                            {
                                foreach (var driver in drivers)
                                {
                                    int carIdx = driver.CarIdx;
                                    currentDrivers[carIdx] = new DriverTelemetry
                                    {
                                        CarIdx = carIdx,
                                        CarNumber = Convert.ToString(driver.CarNumber) ?? "",
                                        DriverName = Convert.ToString(driver.UserName) ?? "",
                                        CarModel = Convert.ToString(driver.CarScreenName) ?? "",
                                        TeamName = Convert.ToString(driver.TeamName) ?? "",
                                        ClassRelativeSpeed = ReadClassRelativeSpeed(parsed, carIdx),
                                        ClassName = Convert.ToString(driver.CarClassShortName),
                                        ClassColor = Convert.ToString(driver.CarClassColor),
                                        IsPaceCar = driver.CarIsPaceCar != 0
                                    };
                                }
                            }

                            Console.WriteLine($"Session information received. Drivers: {currentDrivers.Count}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"SessionInfo processing error: {ex.Message}");
                    }

                    return Task.CompletedTask;
                },

                OnTelemetryUpdate = _ =>
                {
                    // Read all scalar and array values once per telemetry sample.
                    object? positions = client.GetValue("CarIdxPosition");
                    object? classPositions = client.GetValue("CarIdxClassPosition");
                    object? classes = client.GetValue("CarIdxClass");
                    object? carLaps = client.GetValue("CarIdxLap");
                    object? lapsCompleted = client.GetValue("CarIdxLapCompleted");
                    object? lapDistances = client.GetValue("CarIdxLapDistPct");
                    object? lastLapTimes = client.GetValue("CarIdxLastLapTime");
                    object? bestLapTimes = client.GetValue("CarIdxBestLapTime");
                    object? bestLapNumbers = client.GetValue("CarIdxBestLapNum");
                    object? f2Times = client.GetValue("CarIdxF2Time");
                    object? estimatedTimes = client.GetValue("CarIdxEstTime");
                    object? onPitRoadValues = client.GetValue("CarIdxOnPitRoad");
                    object? trackSurfaces = client.GetValue("CarIdxTrackSurface");

                    int? playerCarIdx = ReadInt(client.GetValue("PlayerCarIdx"));
                    int? playerLap = ReadInt(client.GetValue("Lap"));
                    int? playerLapCompleted = ReadInt(client.GetValue("LapCompleted"));
                    float? playerLapDistPct = ReadFloat(client.GetValue("LapDistPct"));
                    float? fuelLevel = ReadFloat(client.GetValue("FuelLevel"));
                    bool playerOnPitRoad = ReadBool(client.GetValue("OnPitRoad")) ?? false;
                    double sessionTime = ReadDouble(client.GetValue("SessionTime")) ?? 0;

                    int? sessionFlags = ReadInt(client.GetValue("SessionFlags"));
                    // iRacing black flag (0x00010000) applies to the local player,
                    // not to all drivers. Do not label other cars with global flags.
                    bool playerBlackFlag = sessionFlags.HasValue &&
                        (sessionFlags.Value & 0x00010000) != 0;

                    var drivers = new List<DriverTelemetry>();

                    lock (driversLock)
                    {
                        foreach (var driver in currentDrivers.Values)
                        {
                            int index = driver.CarIdx;
                            bool onPitRoad = ReadArrayValue<bool>(onPitRoadValues, index) ?? false;
                            int? completed = ReadArrayValue<int>(lapsCompleted, index);
                            int lap = ReadArrayValue<int>(carLaps, index) ?? (completed ?? 0);
                            int completedValue = completed ?? 0;
                            float? lastLap = ReadArrayValue<float>(lastLapTimes, index);

                            var pace = paceTracker.Update(index, completedValue, lastLap, onPitRoad);
                            var pitStop = pitStopTracker.Update(index, onPitRoad, completedValue, sessionTime);

                            driver.OnPitRoad = onPitRoad;
                            driver.HasBlackFlag = playerCarIdx.HasValue &&
                                index == playerCarIdx.Value && playerBlackFlag;
                            driver.CurrentPitRoadSeconds = onPitRoad && pitStop != null && pitStop.IsInProgress
                                ? Math.Max(0, sessionTime - pitStop.EntrySessionTime)
                                : null;
                            driver.Lap = lap;
                            driver.LapCompleted = completedValue;
                            driver.Position = ReadArrayValue<int>(positions, index) ?? 0;
                            driver.ClassPosition = ReadArrayValue<int>(classPositions, index) ?? 0;
                            driver.ClassId = ReadArrayValue<int>(classes, index);
                            driver.LapDistPct = ReadArrayValue<float>(lapDistances, index);
                            driver.LastLapTime = lastLap;
                            driver.BestLapTime = ReadArrayValue<float>(bestLapTimes, index);
                            driver.BestLapNum = ReadArrayValue<int>(bestLapNumbers, index);
                            driver.F2Time = ReadArrayValue<float>(f2Times, index);
                            driver.EstimatedTime = ReadArrayValue<float>(estimatedTimes, index);
                            driver.TrackSurface = ReadArrayValue<int>(trackSurfaces, index);
                            driver.AverageLast3Laps = pace.AverageSeconds;
                            driver.PaceSamples = pace.SampleCount;
                            driver.LastPitStop = pitStop;

                            drivers.Add(CloneDriver(driver));
                        }
                    }

                    int sessionNumber = ReadInt(client.GetValue("SessionNum")) ?? -1;
                    int sessionState = ReadInt(client.GetValue("SessionState")) ?? 0;
                    if (sessionNumber != lastSessionNumber)
                    {
                        lastSessionNumber = sessionNumber;
                        lock (driversLock)
                            currentSessionType = GetSessionType(sessionInfo, sessionNumber);
                        liveOrderTracker.Reset();
                        relativeTracker.Reset();
                    }
                    bool isRace = currentSessionType.Contains("Race", StringComparison.OrdinalIgnoreCase);
                    // On the race grid, show qualifying results until actual racing begins.
                    bool showQualifyingGrid = isRace && sessionState != 4 && sessionState != 6 &&
                        qualifyingTracker.HasResults(sessionNumber - 1) &&
                        !drivers.Any(d => d.LapCompleted >= 0 && d.LapDistPct is >= 0);
                    bool showRaceTiming = isRace && !showQualifyingGrid;
                    if (showRaceTiming)
                    {
                        liveOrderTracker.Update(drivers);
                        relativeTracker.Update(drivers, sessionTime, true);
                    }
                    else
                    {
                        qualifyingTracker.Update(drivers, showQualifyingGrid ? sessionNumber - 1 : sessionNumber, !showQualifyingGrid);
                    }

                    bool isFullCourseCaution = sessionFlags.HasValue &&
                        (sessionFlags.Value & 0x0000C000) != 0;

                    // Fuel consumption is calculated at completed-lap boundaries.
                    // Startup partial laps, pit laps and full-course-caution laps are discarded.
                    var fuelResult = fuelTracker.Update(playerLapCompleted, fuelLevel, playerOnPitRoad, isFullCourseCaution);

                    var player = new PlayerTelemetry
                    {
                        CarIdx = playerCarIdx ?? -1,
                        FuelLevelLiters = fuelLevel,
                        Lap = playerLap,
                        LapCompleted = playerLapCompleted,
                        LapDistPct = playerLapDistPct,
                        CurrentLapTime = ReadFloat(client.GetValue("LapCurrentLapTime")),
                        LastLapTime = ReadFloat(client.GetValue("LapLastLapTime")),
                        BestLapTime = ReadFloat(client.GetValue("LapBestLapTime")),
                        DeltaToBestLap = ReadFloat(client.GetValue("LapDeltaToBestLap")),
                        MyIncidentCount = ReadInt(client.GetValue("PlayerCarMyIncidentCount")),
                        TeamIncidentCount = ReadInt(client.GetValue("PlayerCarTeamIncidentCount")),
                        OnPitRoad = ReadBool(client.GetValue("OnPitRoad")),
                        InPitStall = ReadBool(client.GetValue("PlayerCarInPitStall")),
                        TowTimeSeconds = ReadFloat(client.GetValue("PlayerCarTowTime")),
                        PitstopActive = ReadBool(client.GetValue("PitstopActive")),
                        PitServiceStatus = ReadInt(client.GetValue("PlayerCarPitSvStatus")),
                        PitFuelRequestedLiters = ReadFloat(client.GetValue("PitSvFuel")),
                        PitRepairRemainingSeconds = ReadFloat(client.GetValue("PitRepairLeft")),
                        PitOptionalRepairRemainingSeconds = ReadFloat(client.GetValue("PitOptRepairLeft")),
                        FastRepairAvailable = ReadBool(client.GetValue("FastRepairAvailable")),
                        FastRepairUsed = ReadBool(client.GetValue("FastRepairUsed")),
                        FastRepairsUsed = ReadInt(client.GetValue("PlayerFastRepairsUsed")),
                        Fuel = new FuelAnalysisTelemetry
                        {
                            LastLapUsageLiters = fuelResult.LastLapUsage,
                            AverageLast3LapsLiters = fuelResult.AverageLast3,
                            AverageLast10LapsLiters = fuelResult.AverageLast10,
                            MinimumLapUsageLiters = fuelResult.Minimum,
                            MaximumLapUsageLiters = fuelResult.Maximum,
                            ValidLapSamples = fuelResult.SampleCount,
                            LapsToEmpty = fuelResult.LapsToEmpty
                        }
                    };

                    var sessionTelemetry = new SessionTelemetry
                    {
                        SessionNum = ReadInt(client.GetValue("SessionNum")),
                        SessionState = ReadInt(client.GetValue("SessionState")),
                        SessionFlags = sessionFlags,
                        SessionTick = ReadInt(client.GetValue("SessionTick")),
                        SessionTimeSeconds = ReadDouble(client.GetValue("SessionTime")),
                        SessionTimeRemainSeconds = ReadDouble(client.GetValue("SessionTimeRemain")),
                        SessionTimeTotalSeconds = ReadDouble(client.GetValue("SessionTimeTotal")),
                        SessionLapsRemainEx = ReadInt(client.GetValue("SessionLapsRemainEx")),
                        SessionLapsTotal = ReadInt(client.GetValue("SessionLapsTotal")),
                        PlayerCarIdx = playerCarIdx,
                        IsFullCourseCaution = isFullCourseCaution,
                        AirTemperatureC = ReadFloat(client.GetValue("AirTemp")),
                        TrackTemperatureC = ReadFloat(client.GetValue("TrackTempCrew")),
                        WindVelocity = ReadFloat(client.GetValue("WindVel")),
                        WindDirection = ReadFloat(client.GetValue("WindDir")),
                        TrackWetness = ReadFloat(client.GetValue("TrackWetness")),
                        Precipitation = ReadFloat(client.GetValue("Precipitation")),
                        WeatherDeclaredWet = ReadBool(client.GetValue("WeatherDeclaredWet"))
                    };

                    long now = Stopwatch.GetTimestamp();
                    if (Stopwatch.GetElapsedTime(lastSnapshotTimestamp).TotalMilliseconds >= 250)
                    {
                        lastSnapshotTimestamp = now;
                        player.Tires = ReadTires(client);
                        JsonElement? sessionInfoCopy;
                        lock (driversLock)
                            sessionInfoCopy = sessionInfo?.Clone();

                        publisher.Publish(new ConnectorSnapshot
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Connected = connected,
                            Session = sessionTelemetry,
                            Player = player,
                            Drivers = drivers,
                            SessionInfo = sessionInfoCopy
                        });
                    }

                    if (Stopwatch.GetElapsedTime(lastConsoleTimestamp).TotalSeconds >= 1)
                    {
                        lastConsoleTimestamp = now;
                        RenderTimingTable(drivers, player, showRaceTiming, showQualifyingGrid ? "Qualifying (grid)" : currentSessionType);
                    }

                    return Task.CompletedTask;
                },

                OnError = error =>
                {
                    Console.Error.WriteLine($"iRacing SDK error: {error.Message}");
                    return Task.CompletedTask;
                }
            };

            await client.Monitor(handlers, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Stopping connector...");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FATAL CONNECTOR ERROR:");
            Console.Error.WriteLine(ex);
        }

        Console.WriteLine("Connector stopped.");
    }

    // Only explicitly approved SessionInfo fields are allowed into the public JSON.
    // Camera definitions, car setup/garage data and other SDK metadata are intentionally excluded.
    private static JsonElement BuildFilteredSessionInfo(JsonElement source)
    {
        var result = new JsonObject();

        JsonElement weekendInfo = FindProperty(source, "WeekendInfo");
        if (weekendInfo.ValueKind == JsonValueKind.Object)
        {
            result["weekendInfo"] = CopySelectedObject(weekendInfo,
            [
                "TrackName", "TrackID", "TrackLength", "TrackLengthOfficial",
                "TrackDisplayName", "TrackCity", "TrackCountry", "TrackConfigName",
                "TrackAltitude", "TrackLatitude", "TrackLongitude", "TrackNorth",
                "TrackType", "TrackDirection", "TrackNumTurns"
            ]);
        }

        JsonElement sessionInfo = FindProperty(source, "SessionInfo");
        if (sessionInfo.ValueKind == JsonValueKind.Object)
        {
            var filtered = CopySelectedObject(sessionInfo,
            ["CurrentSessionNum"]);

            JsonElement sessions = FindProperty(sessionInfo, "Sessions");
            if (sessions.ValueKind == JsonValueKind.Array)
            {
                var filteredSessions = new JsonArray();
                foreach (JsonElement item in sessions.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    filteredSessions.Add(CopySelectedObject(item,
                    [
                        "SessionNum", "SessionLaps", "SessionTime", "SessionType",
                        "SessionName", "SessionSubType", "SessionTrackRubber",
                        "SessionEnforceTireCompound", "SessionNumLaps", "SessionTimeOfDay"
                    ]));
                }
                filtered["sessions"] = filteredSessions;
            }

            if (filtered.TryGetPropertyValue("currentSessionNum", out JsonNode? currentSessionNum))
                result["currentSessionNum"] = currentSessionNum?.DeepClone();
            if (filtered.TryGetPropertyValue("sessions", out JsonNode? filteredSessionsNode))
                result["sessions"] = filteredSessionsNode?.DeepClone();
        }

        JsonElement driverInfo = FindProperty(source, "DriverInfo");
        if (driverInfo.ValueKind == JsonValueKind.Object)
        {
            var filtered = CopySelectedObject(driverInfo,
            ["DriverCarIdx", "DriverUserID", "DriverTeamName", "DriverCarIsElectric"]);

            JsonElement drivers = FindProperty(driverInfo, "Drivers");
            if (drivers.ValueKind == JsonValueKind.Array)
            {
                var filteredDrivers = new JsonArray();
                foreach (JsonElement item in drivers.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    filteredDrivers.Add(CopySelectedObject(item,
                    [
                        "CarIdx", "UserName", "UserID", "CarNumber", "CarPath",
                        "CarScreenName", "TeamName", "TeamID", "CarClassID",
                        "CarClassShortName", "CarClassRelSpeed", "CarClassColor",
                        "CarIsPaceCar", "CarIsAI", "IsSpectator"
                    ]));
                }
                filtered["drivers"] = filteredDrivers;
            }

            result["driverInfo"] = filtered;
        }

        return JsonSerializer.SerializeToElement(result, SessionJsonOptions);
    }

    private static JsonObject CopySelectedObject(JsonElement source, string[] allowedProperties)
    {
        var result = new JsonObject();
        foreach (string allowed in allowedProperties)
        {
            JsonElement value = FindProperty(source, allowed);
            if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                continue;

            result[ToCamelCase(allowed)] = JsonNode.Parse(value.GetRawText());
        }
        return result;
    }

    private static JsonElement FindProperty(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object)
            return default;

        foreach (JsonProperty property in source.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }
        return default;
    }

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private static VehicleTiresTelemetry ReadTires(ITelemetryClient<DynamicTelemetryData> client)
    {
        return new VehicleTiresTelemetry
        {
            LF = ReadTire(client, "LF"),
            RF = ReadTire(client, "RF"),
            LR = ReadTire(client, "LR"),
            RR = ReadTire(client, "RR")
        };
    }

    private static TireTelemetry ReadTire(ITelemetryClient<DynamicTelemetryData> client, string corner)
    {
        return new TireTelemetry
        {
            LeftTemp = ReadFloat(client.GetValue($"{corner}tempCL")),
            CenterTemp = ReadFloat(client.GetValue($"{corner}tempCM")),
            RightTemp = ReadFloat(client.GetValue($"{corner}tempCR")),
            LeftWear = ReadFloat(client.GetValue($"{corner}wearL")),
            MiddleWear = ReadFloat(client.GetValue($"{corner}wearM")),
            RightWear = ReadFloat(client.GetValue($"{corner}wearR")),
            ColdPressure = ReadFloat(client.GetValue($"{corner}coldPressure"))
        };
    }

    private static DriverTelemetry CloneDriver(DriverTelemetry source) => new()
    {
        CarIdx = source.CarIdx,
        CarNumber = source.CarNumber,
        DriverName = source.DriverName,
        CarModel = source.CarModel,
        TeamName = source.TeamName,
        ClassId = source.ClassId,
        ClassRelativeSpeed = source.ClassRelativeSpeed,
        ClassName = source.ClassName,
        ClassColor = source.ClassColor,
        IsPaceCar = source.IsPaceCar,
        Position = source.Position,
        ClassPosition = source.ClassPosition,
        Lap = source.Lap,
        LapCompleted = source.LapCompleted,
        LapDistPct = source.LapDistPct,
        LastLapTime = source.LastLapTime,
        BestLapTime = source.BestLapTime,
        BestLapNum = source.BestLapNum,
        F2Time = source.F2Time,
        EstimatedTime = source.EstimatedTime,
        TrackSurface = source.TrackSurface,
        OnPitRoad = source.OnPitRoad,
        HasBlackFlag = source.HasBlackFlag,
        CurrentPitRoadSeconds = source.CurrentPitRoadSeconds,
        AverageLast3Laps = source.AverageLast3Laps,
        PaceSamples = source.PaceSamples,
        GapSeconds = source.GapSeconds,
        IntervalSeconds = source.IntervalSeconds,
        LivePosition = source.LivePosition,
        LiveClassPosition = source.LiveClassPosition,
        QualifyingBestLapTime = source.QualifyingBestLapTime,
        LastPitStop = source.LastPitStop == null ? null : new PitStopTelemetry
        {
            EntryLap = source.LastPitStop.EntryLap,
            ExitLap = source.LastPitStop.ExitLap,
            EntrySessionTime = source.LastPitStop.EntrySessionTime,
            ExitSessionTime = source.LastPitStop.ExitSessionTime,
            PitRoadDurationSeconds = source.LastPitStop.PitRoadDurationSeconds,
            IsInProgress = source.LastPitStop.IsInProgress
        }
    };

    private static T? ReadArrayValue<T>(object? value, int index) where T : struct
    {
        if (value is not Array array || index < 0 || index >= array.Length)
            return null;

        object? item = array.GetValue(index);
        if (item == null)
            return null;

        try
        {
            if (item is T exact)
                return exact;
            return (T)Convert.ChangeType(item, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
        }
        catch
        {
            return null;
        }
    }

    private static float? ReadFloat(object? value)
    {
        if (value == null) return null;
        try
        {
            float result = Convert.ToSingle(value);
            return float.IsFinite(result) ? result : null;
        }
        catch { return null; }
    }

    private static int? ReadInt(object? value)
    {
        if (value == null) return null;
        try { return Convert.ToInt32(value); }
        catch { return null; }
    }

    private static double? ReadDouble(object? value)
    {
        if (value == null) return null;
        try
        {
            double result = Convert.ToDouble(value);
            return double.IsFinite(result) ? result : null;
        }
        catch { return null; }
    }

    private static bool? ReadBool(object? value)
    {
        if (value == null) return null;
        try { return Convert.ToBoolean(value); }
        catch { return null; }
    }

    private static int ReadClassRelativeSpeed(JsonElement root, int carIdx)
    {
        var drivers = FindProperty(FindProperty(root, "DriverInfo"), "Drivers");
        if (drivers.ValueKind != JsonValueKind.Array) return 0;
        foreach (var driver in drivers.EnumerateArray())
        {
            var index = FindProperty(driver, "CarIdx");
            if (index.ValueKind != JsonValueKind.Number || !index.TryGetInt32(out int n) || n != carIdx) continue;
            var speed = FindProperty(driver, "CarClassRelSpeed");
            return speed.ValueKind == JsonValueKind.Number && speed.TryGetInt32(out int value) ? value : 0;
        }
        return 0;
    }

    private static string GetSessionType(JsonElement? info, int number)
    {
        if (!info.HasValue) return "";
        var sessions = FindProperty(info.Value, "sessions");
        if (sessions.ValueKind != JsonValueKind.Array) return "";
        foreach (var session in sessions.EnumerateArray())
        {
            var num = FindProperty(session, "sessionNum");
            if (num.ValueKind == JsonValueKind.Number && num.TryGetInt32(out int n) && n == number)
            {
                var type = FindProperty(session, "sessionType");
                return type.ValueKind == JsonValueKind.String ? type.GetString() ?? "" : "";
            }
        }
        return "";
    }

    private static void RenderTimingTable(List<DriverTelemetry> rows, PlayerTelemetry player, bool isRace, string sessionType)
    {
        Console.WriteLine();
        Console.WriteLine($"================ PIT WALL {(isRace ? "LIVE TIMING" : "SESSION RESULTS")} ================");
        Console.WriteLine($"Session: {sessionType} | Cars: {rows.Count} | Fuel: {player.FuelLevelLiters?.ToString("F2") ?? "N/A"} L");
        Console.WriteLine("------------------------------------------------------------------------------------------------------------");
        Console.WriteLine($"{ "Pos",-5}{ "Cls",-5}{ "Car",-6}{ "Driver",-21}{ "Lap",-6}{ "Gap",-10}{ "Int",-10}{ (isRace ? "Last Lap" : "Best Lap"),-13}{ "Avg 3",-12}{ "Status",-20}");
        Console.WriteLine("------------------------------------------------------------------------------------------------------------");

        // Group classes in practice/qualifying; during a race use current physical order.
        var ordered = isRace
            ? rows.OrderBy(x => x.LivePosition <= 0 ? int.MaxValue : x.LivePosition).ThenBy(x => x.Position)
            : rows.OrderByDescending(x => x.ClassId is >= 0 ? x.ClassRelativeSpeed : int.MinValue)
                  .ThenBy(x => x.ClassId)
                  .ThenBy(x => x.LiveClassPosition <= 0 ? int.MaxValue : x.LiveClassPosition);
        int? lastClass = null;
        foreach (var car in ordered)
        {
            if (!isRace && car.ClassId != lastClass)
            {
                Console.WriteLine($"--- CLASS {car.ClassId?.ToString() ?? "UNKNOWN"} ---");
                lastClass = car.ClassId;
            }
            Console.WriteLine(
                $"{Fit(car.LivePosition > 0 ? car.LivePosition.ToString() : "-", 5)}" +
                $"{Fit(car.LiveClassPosition > 0 ? car.LiveClassPosition.ToString() : "-", 5)}" +
                $"{Fit(car.CarNumber, 6)}" +
                $"{Fit(car.DriverName, 21)}" +
                $"{Fit(car.LapCompleted >= 0 ? car.LapCompleted.ToString() : "-", 6)}" +
                $"{Fit(FormatGap(car.GapSeconds), 10)}" +
                $"{Fit(FormatGap(car.IntervalSeconds), 10)}" +
                $"{Fit(FormatLapTime(isRace ? car.LastLapTime : car.QualifyingBestLapTime), 13)}" +
                $"{Fit(FormatLapTime(car.AverageLast3Laps.HasValue ? (float)car.AverageLast3Laps.Value : null), 12)}" +
                $"{Fit(FormatCarStatus(car), 20)}");
        }
        Console.WriteLine("========================================================");
    }

    private static string FormatCarStatus(DriverTelemetry car)
    {
        var status = new List<string>();
        if (car.HasBlackFlag) status.Add("BLACK FLAG");
        if (car.OnPitRoad)
            status.Add($"PIT {FormatDuration(car.CurrentPitRoadSeconds)}");
        else if (car.LastPitStop != null && !car.LastPitStop.IsInProgress &&
                 car.LastPitStop.PitRoadDurationSeconds.HasValue)
            status.Add($"PIT LAST {FormatDuration(car.LastPitStop.PitRoadDurationSeconds)}");
        return string.Join(" | ", status);
    }

    private static string FormatDuration(double? seconds)
    {
        if (!seconds.HasValue || !double.IsFinite(seconds.Value) || seconds.Value < 0)
            return "--:--";
        var duration = TimeSpan.FromSeconds(seconds.Value);
        return $"{(int)duration.TotalMinutes:00}:{duration.Seconds:00}";
    }

    private static string FormatGap(double? value) =>
        value.HasValue && double.IsFinite(value.Value) ? $"+{value.Value:F1}s" : "-";

    private static string FormatLapTime(float? seconds)
    {
        if (!seconds.HasValue || !float.IsFinite(seconds.Value) || seconds.Value <= 0)
            return "--:--.---";
        return TimeSpan.FromSeconds(seconds.Value).ToString(@"m\:ss\.fff");
    }

    private static string Fit(string value, int width)
    {
        value ??= "";
        return value.Length > width ? value[..width] : value.PadRight(width);
    }
}
