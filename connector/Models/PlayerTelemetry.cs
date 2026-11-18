namespace PitWallConnector.Models;

public sealed class PlayerTelemetry
{
    public int CarIdx { get; set; }
    public float? FuelLevelLiters { get; set; }
    public int? Lap { get; set; }
    public int? LapCompleted { get; set; }
    public float? LapDistPct { get; set; }
    public float? CurrentLapTime { get; set; }
    public float? LastLapTime { get; set; }
    public float? BestLapTime { get; set; }
    public float? DeltaToBestLap { get; set; }
    public int? MyIncidentCount { get; set; }
    public int? TeamIncidentCount { get; set; }
    public bool? OnPitRoad { get; set; }
    public bool? InPitStall { get; set; }
    public float? TowTimeSeconds { get; set; }
    public bool? PitstopActive { get; set; }
    public int? PitServiceStatus { get; set; }
    public float? PitFuelRequestedLiters { get; set; }
    public float? PitRepairRemainingSeconds { get; set; }
    public float? PitOptionalRepairRemainingSeconds { get; set; }
    public bool? FastRepairAvailable { get; set; }
    public bool? FastRepairUsed { get; set; }
    public int? FastRepairsUsed { get; set; }

    public FuelAnalysisTelemetry Fuel { get; set; } = new();
    public VehicleTiresTelemetry Tires { get; set; } = new();
}

public sealed class FuelAnalysisTelemetry
{
    public float? LastLapUsageLiters { get; set; }
    public float? AverageLast3LapsLiters { get; set; }
    public float? AverageLast10LapsLiters { get; set; }
    public float? MinimumLapUsageLiters { get; set; }
    public float? MaximumLapUsageLiters { get; set; }
    public int ValidLapSamples { get; set; }
    public float? LapsToEmpty { get; set; }
}
