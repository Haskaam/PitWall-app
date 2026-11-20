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
    livePosition: number;
    liveClassPosition: number;
    gapSeconds: number | null;
    intervalSeconds: number | null;
    lastLapTime: number;
    bestLapTime: number;
    onPitRoad: boolean;
};

export type TelemetryResponse = {
    fresh: boolean;
    ageSeconds: number;
    receivedAtUtc: string;
    snapshot: TelemetrySnapshot;
}