namespace PitWallConnector.Services;

public sealed record PaceResult(
    double? AverageSeconds,
    int SampleCount
);

public sealed class PaceTracker
{
    private const int MaxSamples = 3;

    private sealed class CarPaceState
    {
        public int LastLapCompleted { get; set; }

        public bool WasOnPitRoad { get; set; }

        public Queue<double> LapTimes { get; } = new();
    }

    private readonly Dictionary<int, CarPaceState> _cars = new();

    public PaceResult Update(
        int carIdx,
        int lapCompleted,
        double? lastLapTime,
        bool onPitRoad)
    {
        if (!_cars.TryGetValue(carIdx, out var state))
        {
            state = new CarPaceState
            {
                LastLapCompleted = lapCompleted
            };

            _cars[carIdx] = state;
        }

        // Samochód znajduje się w alei serwisowej.
        if (onPitRoad)
        {
            // Exclude the pit lap, but preserve previously completed clean laps.
            state.LastLapCompleted = lapCompleted;
            state.WasOnPitRoad = true;

            return GetResult(state);
        }

        // Pierwsza aktualizacja po wyjeździe z pit lane.
        if (state.WasOnPitRoad)
        {
            state.LastLapCompleted = lapCompleted;
            state.WasOnPitRoad = false;

            return GetResult(state);
        }

        // Zabezpieczenie na wypadek zmiany sesji lub resetu licznika.
        if (lapCompleted < state.LastLapCompleted)
        {
            state.LapTimes.Clear();
            state.LastLapCompleted = lapCompleted;

            return GetResult(state);
        }

        // Nowe ukończone okrążenie.
        if (lapCompleted > state.LastLapCompleted)
        {
            state.LastLapCompleted = lapCompleted;

            if (lastLapTime.HasValue &&
                double.IsFinite(lastLapTime.Value) &&
                lastLapTime.Value > 0 &&
                lastLapTime.Value < 3600)
            {
                state.LapTimes.Enqueue(lastLapTime.Value);

                while (state.LapTimes.Count > MaxSamples)
                {
                    state.LapTimes.Dequeue();
                }
            }
        }

        return GetResult(state);
    }

    private static PaceResult GetResult(CarPaceState state)
    {
        if (state.LapTimes.Count == 0)
        {
            return new PaceResult(null, 0);
        }

        double average = state.LapTimes.Average();

        return new PaceResult(
            average,
            state.LapTimes.Count
        );
    }
}