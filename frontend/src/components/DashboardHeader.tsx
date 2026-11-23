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
            <div className="header-brand">
                <h1>Pit Wall</h1>
            </div>

            <div className="header-stats">
                <div className="header-stat">
                    <span className="stat-label">Session Type:</span>
                    <strong className="stat-value">{sessionType ?? " -"}</strong>
                </div>

                <div className="header-stat">
                    <span className="stat-label">Time remaining:</span>
                    <strong className="stat-value">{formatTime(timeRemaining) ?? " -"}</strong>
                </div>

                <div className="header-stat">
                    <span className="stat-label">AirTemp:</span>
                    <strong className="stat-value">{airTemp?.toFixed(1) ?? " -"}</strong>
                </div>

                <div className="header-stat">
                    <span className="stat-label">TrackTemp:</span>
                    <strong className="stat-value">{trackTemp?.toFixed(1) ?? " -"}</strong>
                </div>
            </div>
        </header>
    );
}

export default DashboardHeader;