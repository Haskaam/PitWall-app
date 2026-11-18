using System.Text.Json;
using PitWallConnector.Models;

namespace PitWallConnector.Services;

/// <summary>Best lap classification for non-race sessions. Keeps times through session transitions.</summary>
public sealed class QualifyingTracker
{
    private readonly Dictionary<(int Session, int Car), float> _best = new();
    private readonly Dictionary<(int Session, int Car), float> _official = new();

    public void ReadOfficialResults(JsonElement root)
    {
        // SessionInfo.Sessions[].ResultsPositions: official timing when supplied by SDK.
        var info = Get(root, "SessionInfo");
        var sessions = Get(info, "Sessions");
        if (sessions.ValueKind != JsonValueKind.Array) return;
        foreach (var session in sessions.EnumerateArray())
        {
            int? number = Int(Get(session, "SessionNum"));
            var positions = Get(session, "ResultsPositions");
            if (!number.HasValue || positions.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in positions.EnumerateArray())
            {
                int? car = Int(Get(item, "CarIdx"));
                float? time = Float(Get(item, "FastestTime"));
                if (car.HasValue && time is > 0 and < 3600)
                    _official[(number.Value, car.Value)] = time.Value;
            }
        }
    }

    public bool HasResults(int session) => _best.Keys.Any(x => x.Session == session) ||
        _official.Keys.Any(x => x.Session == session);

    public void Update(List<DriverTelemetry> drivers, int session, bool collect = true)
    {
        foreach (var car in drivers)
        {
            if (collect && car.BestLapTime is > 0 and < 3600 && float.IsFinite(car.BestLapTime.Value))
            {
                var key = (session, car.CarIdx);
                if (!_best.TryGetValue(key, out var old) || car.BestLapTime.Value < old)
                    _best[key] = car.BestLapTime.Value;
            }
            var lookup = (session, car.CarIdx);
            car.QualifyingBestLapTime = _official.TryGetValue(lookup, out var official) ? official :
                _best.TryGetValue(lookup, out var best) ? best : null;
            car.GapSeconds = null;
            car.IntervalSeconds = null;
            car.LivePosition = 0;
            car.LiveClassPosition = 0;
        }

        var eligible = drivers.Where(x => x.ClassId is >= 0 && x.Position >= 0).ToList();
        var groups = eligible.GroupBy(x => x.ClassId).OrderByDescending(g => g.Max(x => x.ClassRelativeSpeed));
        int overall = 1;
        foreach (var group in groups)
        {
            var sorted = group.OrderBy(x => x.QualifyingBestLapTime is > 0 ? x.QualifyingBestLapTime : float.PositiveInfinity)
                .ThenBy(x => x.ClassPosition > 0 ? x.ClassPosition : int.MaxValue).ToList();
            float? leader = sorted.FirstOrDefault(x => x.QualifyingBestLapTime is > 0)?.QualifyingBestLapTime;
            float? previous = null;
            int classPos = 1;
            foreach (var car in sorted)
            {
                car.LivePosition = overall++;
                car.LiveClassPosition = classPos++;
                if (car.QualifyingBestLapTime is > 0 && leader.HasValue)
                {
                    car.GapSeconds = Math.Max(0, car.QualifyingBestLapTime.Value - leader.Value);
                    if (previous.HasValue)
                        car.IntervalSeconds = Math.Max(0, car.QualifyingBestLapTime.Value - previous.Value);
                    previous = car.QualifyingBestLapTime;
                }
            }
        }
    }

    private static JsonElement Get(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return default;
        foreach (var property in obj.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        return default;
    }
    private static int? Int(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n) ? n : null;
    private static float? Float(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float n) ? n : null;
}
