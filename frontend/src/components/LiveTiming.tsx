import { Fragment } from "react";
import type { Driver } from "../types/telemetry";
import { formatLapTime } from "../utlis/formatTime";

type LiveTimingProps = {
    drivers: Driver[];
};

function LiveTiming({drivers}: LiveTimingProps) {
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
                    </tr>
                </thead>

                <tbody>
                    {groupedDrivers.map((group, index) => (
                        <Fragment key={group[0].classId ?? `unknown-${index}`}>
                            <tr className="class-header">
                                <td colSpan={7}>
                                    {group[0].className || group.map(driver => getClassName(driver.carModel)).find(Boolean) || `${group[0].classId ?? "UNKOWN"}`}
                                </td>
                            </tr>

                            {group.map(driver => (
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
                                    <td>{formatLapTime(driver.lastLapTime)}</td>
                                    <td>{formatLapTime(driver.qualifyingBestLapTime ?? driver.bestLapTime)}</td>
                                </tr>
                            ))}
                        </Fragment>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

export default LiveTiming;