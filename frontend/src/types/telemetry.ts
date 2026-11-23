export type TelemetrySnapshot = {
    connected: boolean;

    player: {
        carIdx: number;
        fuelLevelLiters: number;
        lap: number;
        lapCompleted: number;
    };

    session: {
        sessionTimeRemainSeconds: number;
        sessionTimeTotalSeconds: number;
        airTemperatureC: number;
        trackTemperatureC: number;
    };

    drivers: Driver[];
};

export type Driver = {
    carIdx: number;
    carNumber: string;
    driverName: string;
    carModel: string;
    classId: number;
    className: string | null;
    classColor: string | null;
    isPaceCar: boolean;
    classRelativeSpeed: number;
    livePosition: number;
    liveClassPosition: number;
    gapSeconds: number | null;
    intervalSeconds: number | null;
    lastLapTime: number;
    bestLapTime: number;
    qualifyingBestLapTime: number | null;
    onPitRoad: boolean;
};

export type TelemetryResponse = {
    fresh: boolean;
    ageSeconds: number;
    receivedAtUtc: string;
    snapshot: TelemetrySnapshot;
}