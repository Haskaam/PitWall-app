import { Fragment, useEffect, useRef, useState } from "react";
import type { Driver } from "../types/telemetry";
import { formatTime } from "../utlis/formatTime";

type LiveTimingProps = {
    drivers: Driver[];
};

type LapColor = "lap-purple" | "lap-green" | "";

type LapHistory = {
    lastLapTime: number;
    bestLapTime: number;
    color: LapColor;
}

function LiveTiming({drivers}: LiveTimingProps) {
    const lapHistory = useRef<Record<number, LapHistory>>({});
    const [lapColor, setLapColor] = useState<Record<number, LapColor>>({});
    const raceDrivers = drivers.filter(driver => !driver.isPaceCar && driver.classId !== null && driver.classId >= 0);

    const groupedDrivers = Object.values(
        raceDrivers.reduce<Record<string, Driver[]>>((groups, driver) => {
            const classId = String(driver.classId ?? "Uknown");

            if(!groups[classId]) {
                groups[classId] = [];
            }

            groups[classId].push(driver);

            return groups;
        }, {})
    );

    groupedDrivers.sort((a, b) => 
        b[0].classRelativeSpeed - a[0].classRelativeSpeed
    );

    groupedDrivers.forEach(group => {
        group.sort((a, b) => {
            const posA = a.liveClassPosition > 0 ? a.liveClassPosition : Infinity;
            const posB = b.liveClassPosition > 0 ? b.liveClassPosition : Infinity;

            return posA - posB;
        });
    });

    function getClassName(carModel: string): string | null {
    const model = carModel.toUpperCase();

    

    if (model.includes("GT3")) return "GT3";
    if (model.includes("LMP2")) return "LMP2";
    if (model.includes("GTP") || model.includes("LMH")) return "GTP";

    return null;
}

useEffect(() => {
    const nextColors: Record<number, LapColor> = {};
    let hasChanges = false;

    groupedDrivers.forEach(group => {
        const validTimes = group
            .map(driver => driver.bestLapTime)
            .filter(time => Number.isFinite(time) && time > 0);

        const classBestLap = Math.min(...validTimes);

        group.forEach(driver => {
            const previous = lapHistory.current[driver.carIdx];

            const lastTime = driver.lastLapTime;
            const bestTime = driver.bestLapTime;

            if (!Number.isFinite(lastTime) || lastTime <= 0) {
                return;
            }

            if (!previous) {
                lapHistory.current[driver.carIdx] = {
                    lastLapTime: lastTime,
                    bestLapTime: bestTime,
                    color: ""
                };

                return;
            }

            if (lastTime !== previous.lastLapTime) {
                const isPersonalBest =
                    bestTime > 0 &&
                    lastTime === bestTime &&
                    (
                        previous.bestLapTime <= 0 ||
                        bestTime < previous.bestLapTime
                    );

                const isClassBest =
                    isPersonalBest &&
                    lastTime === classBestLap;

                const color: LapColor = isClassBest
                    ? "lap-purple"
                    : isPersonalBest
                        ? "lap-green"
                        : "";

                lapHistory.current[driver.carIdx] = {
                    lastLapTime: lastTime,
                    bestLapTime: bestTime,
                    color
                };

                hasChanges = true;
            } else {
                previous.bestLapTime = bestTime;
            }

            nextColors[driver.carIdx] =
                lapHistory.current[driver.carIdx].color;
        });
    });

    if (hasChanges) {
        setLapColor(nextColors);
    }
}, [drivers]);

    return (    
        <div className="live-timing">
            <table>
                <thead>
                    <tr>
                        <th>POS</th>
                        <th>CAR</th>
                        <th>DRIVER</th>
                        <th>GAP</th>
                        <th>INTERVAL</th>
                        <th>LAST</th>
                        <th>BEST</th>
                        <th>PIT</th>
                    </tr>
                </thead>

                <tbody>
                    {groupedDrivers.map((group, index) => {
                        const classBestLap = Math.min(
                            ...group.map(driver => driver.bestLapTime).filter(time => time > 0)
                        );
                        return (
                        <Fragment key={group[0].classId ?? `unknown-${index}`}>
                            <tr className="class-header">
                                <td colSpan={8}>
                                    {group[0].className || group.map(driver => getClassName(driver.carModel)).find(Boolean) || `${group[0].classId ?? "UNKOWN"}`}
                                </td>
                            </tr>

                            {group.map(driver => {
                                const isValidLap = driver.lastLapTime > 0;
                                const isClassBest = isValidLap && driver.lastLapTime === classBestLap;
                                const isPersonalBest = isValidLap && driver.lastLapTime === driver.bestLapTime;
                                const lastLapClass = isClassBest ? "lap-purple" : isPersonalBest ? "lap-green" : "";

                                return (
                                <tr key={driver.carIdx}>
                                    <td>{driver.liveClassPosition > 0 ? driver.liveClassPosition : "-"}</td>
                                    <td>{driver.carNumber}</td>
                                    <td>{driver.driverName}</td>
                                    <td>
                                        {driver.gapSeconds !== null
                                            ? `+${driver.gapSeconds.toFixed(3)}`
                                            : "-"}
                                    </td>
                                    <td>
                                        {driver.intervalSeconds !== null
                                            ? `+${driver.intervalSeconds.toFixed(3)}`
                                            : "-"}
                                    </td>
                                    <td className={lapColor[driver.carIdx] ?? ""}>
                                        {formatTime(driver.lastLapTime)}
                                    </td>
                                    <td className={driver.bestLapTime > 0 && driver.bestLapTime === classBestLap ? "lap-purple" : ""}>
                                        {formatTime(driver.qualifyingBestLapTime ?? driver.bestLapTime)}
                                    </td>
                                    <td>
                                        {driver.onPitRoad || driver.lastPitStop?.pitRoadDurationSeconds != null
                                        ? (
                                            <span className={driver.onPitRoad ? "pit-timer active" : "pit-timer"}>
                                             {driver.onPitRoad
                                                    ? formatTime(driver.currentPitRoadSeconds)
                                                    : formatTime(driver.lastPitStop?.pitRoadDurationSeconds ?? null)
                                                }
                                            </span>
                                            ) : "-"
                                        }
                                    </td>
                                </tr>
                                );
                            })}
                        </Fragment>
                        );
                    })}
                </tbody>
            </table>
        </div>
    );
}

export default LiveTiming;