namespace PitWallConnector.Models;

public sealed class PitStopTelemetry
{
    public int EntryLap { get; set; }
    public int? ExitLap { get; set; }
    public double EntrySessionTime { get; set; }
    public double? ExitSessionTime { get; set; }
    public double? PitRoadDurationSeconds { get; set; }
    public bool IsInProgress { get; set; }
}
