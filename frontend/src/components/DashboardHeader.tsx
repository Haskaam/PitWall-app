type DashboardHeaderProps = {
    airTemp: number | null;
    trackTemp: number | null;
    sessionType: string | null;
    timeRemaining: number | null;
}

function formatTime(timeRemaining: number | null): string {
    if(timeRemaining === null) return "-";

    const totalSeconds = Math.max(0, Math.floor(timeRemaining));

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor(totalSeconds / 60) % 60;
    const seconds = totalSeconds % 60;

    return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function DashboardHeader({airTemp, trackTemp, sessionType, timeRemaining}: DashboardHeaderProps) {
    return (
        <header className="dashboard-header">
            <h1>Pit Wall</h1>

            <p>Session Type: {sessionType ?? "-"}</p>
            <p>Time Remaining: {formatTime(timeRemaining)}</p>
            <p>AirTemp: {airTemp?.toFixed(1) ?? "-"} °C</p>
            <p>TrackTemp: {trackTemp?.toFixed(1) ?? "-"} °C</p>

        </header>
    );
}

export default DashboardHeader;