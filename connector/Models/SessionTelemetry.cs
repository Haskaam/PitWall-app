namespace PitWallConnector.Models;

public sealed class SessionTelemetry
{
    public int? SessionNum { get; set; }
    public int? SessionState { get; set; }
    public int? SessionFlags { get; set; }
    public int? SessionTick { get; set; }
    public double? SessionTimeSeconds { get; set; }
    public double? SessionTimeRemainSeconds { get; set; }
    public double? SessionTimeTotalSeconds { get; set; }
    public int? SessionLapsRemainEx { get; set; }
    public int? SessionLapsTotal { get; set; }
    public int? PlayerCarIdx { get; set; }
    public bool IsFullCourseCaution { get; set; }

    public float? AirTemperatureC { get; set; }
    public float? TrackTemperatureC { get; set; }
    public float? WindVelocity { get; set; }
    public float? WindDirection { get; set; }
    public float? TrackWetness { get; set; }
    public float? Precipitation { get; set; }
    public bool? WeatherDeclaredWet { get; set; }
}
