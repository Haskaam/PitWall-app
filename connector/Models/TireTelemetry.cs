namespace PitWallConnector.Models;

public sealed class TireTelemetry
{
    public float? LeftTemp { get; set; }
    public float? CenterTemp { get; set; }
    public float? RightTemp { get; set; }
    public float? LeftWear { get; set; }
    public float? MiddleWear { get; set; }
    public float? RightWear { get; set; }
    public float? ColdPressure { get; set; }
}

public sealed class VehicleTiresTelemetry
{
    public TireTelemetry LF { get; set; } = new();
    public TireTelemetry RF { get; set; } = new();
    public TireTelemetry LR { get; set; } = new();
    public TireTelemetry RR { get; set; } = new();
}
