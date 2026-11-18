namespace PitWallConnector.Models;

public sealed class DriverTelemetry
{
    public int CarIdx { get; set; }
    public string CarNumber { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string CarModel { get; set; } = "";
    public string TeamName { get; set; } = "";

    public int? ClassId { get; set; }
    public int ClassRelativeSpeed { get; set; }
    public int Position { get; set; }
    public int ClassPosition { get; set; }
    // Official standings are kept intact; live positions update on overtakes.
    public int LivePosition { get; set; }
    public int LiveClassPosition { get; set; }
    public float? QualifyingBestLapTime { get; set; }
    public int Lap { get; set; }
    public int LapCompleted { get; set; }
    public float? LapDistPct { get; set; }

    public float? LastLapTime { get; set; }
    public float? BestLapTime { get; set; }
    public int? BestLapNum { get; set; }
    public float? F2Time { get; set; }
    public float? EstimatedTime { get; set; }

    // Raw iRacing enum values. Their enum names are deliberately not guessed.
    public int? TrackSurface { get; set; }

    public bool OnPitRoad { get; set; }
    public bool HasBlackFlag { get; set; }
    public double? CurrentPitRoadSeconds { get; set; }
    public double? AverageLast3Laps { get; set; }
    public int PaceSamples { get; set; }
    public double? GapSeconds { get; set; }
    public double? IntervalSeconds { get; set; }
    public PitStopTelemetry? LastPitStop { get; set; }
}
