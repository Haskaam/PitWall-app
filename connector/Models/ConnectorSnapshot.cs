using System.Text.Json;

namespace PitWallConnector.Models;

public sealed class ConnectorSnapshot
{
    public int SchemaVersion { get; set; } = 1;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public bool Connected { get; set; }
    public SessionTelemetry Session { get; set; } = new();
    public PlayerTelemetry Player { get; set; } = new();
    public List<DriverTelemetry> Drivers { get; set; } = [];

    // Whitelisted SessionInfo metadata only; camera and car setup data are excluded.
    public JsonElement? SessionInfo { get; set; }
}
