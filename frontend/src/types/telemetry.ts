export type TelemetrySnapshot = {
    connected: boolean;

    player: {
        carIdx: number;
        fuelLevelLiters: number;
        lap: number;
        lapCompleted: number;
    };

    session: {
        sessionNum: number;
        sessionTimeRemainSeconds: number;
        sessionTimeTotalSeconds: number;
        airTemperatureC: number;
        trackTemperatureC: number;
    };

    sessionInfo: {
        sessions: {
            sessionNum: number;
            sessionName: string;
        }[];
    } | null;

    drivers: Driver[];
};

export type PitStop = {
    pitRoadDurationSeconds: number | null;
    isInProgress: boolean;
}

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
    currentPitRoadSeconds: number | null;
    hasBlackFlag: boolean;
    lastPitStop: PitStop | null;
};

export type TelemetryResponse = {
    fresh: boolean;
    ageSeconds: number;
    receivedAtUtc: string;
    snapshot: TelemetrySnapshot;
}