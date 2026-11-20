export function formatLapTime(seconds: number | null): string {
    if(seconds === null || seconds <= 0 || !Number.isFinite(seconds)) {
        return "-";
    }

    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = (seconds % 60).toFixed(3).padStart(6, "0");

    return `${minutes}:${remainingSeconds}`;
}