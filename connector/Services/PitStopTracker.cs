using PitWallConnector.Models;

namespace PitWallConnector.Services;

public sealed class PitStopTracker
{
    private sealed class State
    {
        public bool WasOnPitRoad { get; set; }
        public PitStopTelemetry? LastStop { get; set; }
    }

    private readonly Dictionary<int, State> _states = new();

    public PitStopTelemetry? Update(int carIdx, bool onPitRoad, int lap, double sessionTime)
    {
        if (!_states.TryGetValue(carIdx, out var state))
        {
            state = new State();
            _states[carIdx] = state;
        }

        if (onPitRoad && !state.WasOnPitRoad)
        {
            state.LastStop = new PitStopTelemetry
            {
                EntryLap = lap,
                EntrySessionTime = sessionTime,
                IsInProgress = true
            };
        }
        else if (!onPitRoad && state.WasOnPitRoad && state.LastStop != null)
        {
            state.LastStop.ExitLap = lap;
            state.LastStop.ExitSessionTime = sessionTime;
            state.LastStop.PitRoadDurationSeconds = Math.Max(0, sessionTime - state.LastStop.EntrySessionTime);
            state.LastStop.IsInProgress = false;
        }

        state.WasOnPitRoad = onPitRoad;
        return state.LastStop;
    }
}
