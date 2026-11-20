type FuelTelemetryProps = {
    fuel: number | null;
    connected: boolean;
}

function FuelTelemetry({fuel, connected}: FuelTelemetryProps) {
    return (
        <div>
            <p>Status: {connected ? "LIVE" : "OFFLINE"}</p>
            <h3>Fuel: {fuel !== null ? `${fuel.toFixed(2)} L` : "N/A"}</h3>
        </div>
    );
}

export default FuelTelemetry;