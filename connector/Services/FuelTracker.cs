namespace PitWallConnector.Services;

public sealed record FuelUsageResult(
    float? LastLapUsage,
    float? AverageLast3,
    float? AverageLast10,
    float? Minimum,
    float? Maximum,
    int SampleCount,
    float? LapsToEmpty);

public sealed class FuelTracker
{
    private const int MaxSamples = 20;
    private readonly Queue<float> _samples = new();
    private int? _boundaryLap;
    private float? _boundaryFuel;
    private bool _pitRoadDuringLap;
    private bool _cautionDuringLap;
    private bool _hasCompletedBoundary;
    private float? _lastUsage;

    public FuelUsageResult Update(int? lapCompleted, float? fuelLevel, bool onPitRoad, bool isCaution)
    {
        if (onPitRoad)
            _pitRoadDuringLap = true;
        if (isCaution)
            _cautionDuringLap = true;

        if (!lapCompleted.HasValue || !IsValidFuel(fuelLevel))
            return BuildResult(fuelLevel);

        if (!_boundaryLap.HasValue)
        {
            _boundaryLap = lapCompleted.Value;
            _boundaryFuel = fuelLevel;
            _pitRoadDuringLap = onPitRoad;
            _cautionDuringLap = isCaution;
            return BuildResult(fuelLevel);
        }

        if (lapCompleted.Value < _boundaryLap.Value)
        {
            Reset(lapCompleted.Value, fuelLevel);
            return BuildResult(fuelLevel);
        }

        if (lapCompleted.Value > _boundaryLap.Value)
        {
            int completedDelta = lapCompleted.Value - _boundaryLap.Value;
            bool cleanSingleLap = completedDelta == 1 && !_pitRoadDuringLap && !onPitRoad && !_cautionDuringLap && !isCaution;

            // The first observed lap transition establishes a complete-lap baseline.
            // This avoids treating the partial lap at connector startup as a full lap.
            if (_hasCompletedBoundary && cleanSingleLap && IsValidFuel(_boundaryFuel))
            {
                float usage = _boundaryFuel!.Value - fuelLevel!.Value;
                // Reject refuelling, negative consumption and implausible jumps.
                if (float.IsFinite(usage) && usage > 0.01f && usage < 100f)
                {
                    _samples.Enqueue(usage);
                    while (_samples.Count > MaxSamples)
                        _samples.Dequeue();
                    _lastUsage = usage;
                }
            }

            _boundaryLap = lapCompleted.Value;
            _boundaryFuel = fuelLevel;
            _pitRoadDuringLap = onPitRoad;
            _cautionDuringLap = isCaution;
            _hasCompletedBoundary = true;
        }
        else
        {
            // Keep the latest boundary fuel, not every 60 Hz fuel sample.
        }

        return BuildResult(fuelLevel);
    }

    private FuelUsageResult BuildResult(float? currentFuel)
    {
        float? avg3 = AverageTail(3);
        float? avg10 = AverageTail(10);
        float? average = avg10;
        float? lapsToEmpty = IsValidFuel(currentFuel) && average > 0
            ? currentFuel!.Value / average.Value
            : null;

        return new FuelUsageResult(
            _lastUsage,
            avg3,
            avg10,
            _samples.Count > 0 ? _samples.Min() : null,
            _samples.Count > 0 ? _samples.Max() : null,
            _samples.Count,
            lapsToEmpty);
    }

    private float? AverageTail(int count)
    {
        var values = _samples.TakeLast(count).ToArray();
        return values.Length == 0 ? null : values.Average();
    }

    private static bool IsValidFuel(float? value) =>
        value.HasValue && float.IsFinite(value.Value) && value.Value >= 0;

    private void Reset(int lap, float? fuel)
    {
        _samples.Clear();
        _lastUsage = null;
        _boundaryLap = lap;
        _boundaryFuel = fuel;
        _pitRoadDuringLap = false;
        _cautionDuringLap = false;
        _hasCompletedBoundary = false;
    }
}
