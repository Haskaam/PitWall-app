using PitWallConnector.Models;

namespace PitWallConnector.Services;

/// <summary>
/// Estimates continuously changing relative gaps from each car's progress around the lap.
/// Speed is derived from successive CarIdxLapDistPct/LapCompleted samples, with last-lap
/// time as a fallback until enough live samples have been collected.
/// </summary>
public sealed class RelativeTracker
{
    private const int SpeedWindow = 5;
    private const double MaxSampleAgeSeconds = 2.0;
    private const double MaxPlausibleLapRate = 0.10; // laps per second (~10 s/lap)

    private sealed class CarState
    {
        public double? PreviousProgress { get; set; }
        public double? PreviousSessionTime { get; set; }
        public Queue<double> LapRates { get; } = new();
    }

    private readonly Dictionary<int, CarState> _states = new();

    public void Reset() => _states.Clear();

    public void Update(IReadOnlyList<DriverTelemetry> drivers, double sessionTime, bool useLivePositions = false)
    {
        foreach (var car in drivers)
        {
            car.GapSeconds = null;
            car.IntervalSeconds = null;

            if (!_states.TryGetValue(car.CarIdx, out var state))
            {
                state = new CarState();
                _states[car.CarIdx] = state;
            }

            if (car.OnPitRoad || !car.LapDistPct.HasValue ||
                !double.IsFinite(car.LapDistPct.Value) ||
                car.LapDistPct.Value < 0 || car.LapDistPct.Value >= 1 ||
                car.LapCompleted < 0)
            {
                state.PreviousProgress = null;
                state.PreviousSessionTime = sessionTime;
                state.LapRates.Clear();
                continue;
            }

            double progress = car.LapCompleted + car.LapDistPct.Value;

            if (state.PreviousProgress.HasValue && state.PreviousSessionTime.HasValue)
            {
                double elapsed = sessionTime - state.PreviousSessionTime.Value;
                double progressDelta = progress - state.PreviousProgress.Value;

                if (elapsed > 0 && elapsed <= MaxSampleAgeSeconds &&
                    progressDelta > 0 && progressDelta / elapsed <= MaxPlausibleLapRate)
                {
                    state.LapRates.Enqueue(progressDelta / elapsed);
                    while (state.LapRates.Count > SpeedWindow)
                        state.LapRates.Dequeue();
                }
                else if (elapsed > MaxSampleAgeSeconds || progressDelta < 0)
                {
                    state.LapRates.Clear();
                }
            }

            state.PreviousProgress = progress;
            state.PreviousSessionTime = sessionTime;
        }

        // The SDK is the authority for race order. Relative distances never reorder standings.
        // Accumulate adjacent intervals to keep class gaps monotonic and consistent.
        var classes = drivers
            .Where(car => car.ClassId.HasValue && (useLivePositions ? car.LiveClassPosition : car.ClassPosition) > 0 &&
                          car.Position > 0 && car.ClassId.Value >= 0)
            .GroupBy(car => car.ClassId!.Value);

        foreach (var classGroup in classes)
        {
            var ordered = classGroup.OrderBy(car => useLivePositions ? car.LiveClassPosition : car.ClassPosition).ToList();
            if (ordered.Count == 0) continue;
            double cumulativeGap = 0;
            bool cumulativeValid = true;
            DriverTelemetry? ahead = ordered[0];

            for (int i = 1; i < ordered.Count; i++)
            {
                var car = ordered[i];
                // A car in pit lane stays in standings, but its moving-track relative
                // is unavailable. The next car must not bridge over it as if absent.
                double? aheadProgress = ahead == null || ahead.OnPitRoad ? null : GetProgress(ahead);
                double? currentProgress = car.OnPitRoad ? null : GetProgress(car);
                double? rate = GetLapRate(car);
                if (aheadProgress.HasValue && currentProgress.HasValue &&
                    rate.HasValue && rate.Value > 0)
                {
                    double distance = aheadProgress.Value - currentProgress.Value;
                    // During timing transitions official standings may lag physical
                    // track order. Do not invent a negative time gap.
                    if (distance >= -0.00004)
                    {
                        double interval = Math.Max(0, distance) / rate.Value;
                        if (double.IsFinite(interval))
                        {
                            car.IntervalSeconds = interval;
                            if (cumulativeValid)
                            {
                                cumulativeGap += interval;
                                car.GapSeconds = cumulativeGap;
                            }
                        }
                        else cumulativeValid = false;
                    }
                    else cumulativeValid = false;
                }
                else
                {
                    // Side-by-side cars can have no speed history yet. A near-zero
                    // physical separation is still meaningful as a displayed 0.0s.
                    if (aheadProgress.HasValue && currentProgress.HasValue &&
                        Math.Abs(aheadProgress.Value - currentProgress.Value) <= 0.00004)
                    {
                        car.IntervalSeconds = 0;
                        if (cumulativeValid) car.GapSeconds = cumulativeGap;
                    }
                    else cumulativeValid = false;
                }

                ahead = car;
            }
        }
    }

    private double? GetLapRate(DriverTelemetry car)
    {
        if (_states.TryGetValue(car.CarIdx, out var state) && state.LapRates.Count > 0)
        {
            // Median is less sensitive to a single noisy telemetry frame than the mean.
            var rates = state.LapRates.OrderBy(x => x).ToArray();
            return rates[rates.Length / 2];
        }

        if (car.LastLapTime.HasValue && double.IsFinite(car.LastLapTime.Value) &&
            car.LastLapTime.Value > 0 && car.LastLapTime.Value < 3600)
            return 1.0 / car.LastLapTime.Value;

        return null;
    }

    private static double? GetProgress(DriverTelemetry car)
    {
        if (!car.LapDistPct.HasValue || !float.IsFinite(car.LapDistPct.Value) ||
            car.LapDistPct.Value < 0 || car.LapDistPct.Value >= 1 || car.LapCompleted < 0)
            return null;

        return car.LapCompleted + car.LapDistPct.Value;
    }
}
