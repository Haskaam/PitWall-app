import type { Driver } from "../types/telemetry";
import { formatLapTime } from "../utlis/formatTime";

type LiveTimingProps = {
    drivers: Driver[];
};

function LiveTiming({drivers}: LiveTimingProps) {
    const sortedDrivers = [...drivers].sort(
        (a, b) => a.livePosition - b.livePosition
    );
    
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
                    {sortedDrivers.map((driver) => (
                        <tr key={driver.carIdx}>
                            <td>{driver.livePosition}</td>
                            <td>{driver.carNumber}</td>
                            <td>{driver.driverName}</td>
                            <td>{driver.gapSeconds !== null
                                ? `+${driver.gapSeconds.toFixed(3)}`
                                : "-"}</td>
                            <td>{driver.intervalSeconds !== null
                                ? `+${driver.intervalSeconds.toFixed(3)}`
                                : "-"}</td>
                            <td>{formatLapTime(driver.lastLapTime)}</td>
                            <td>{formatLapTime(driver.bestLapTime)}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

export default LiveTiming;