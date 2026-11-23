type FuelTelemetryProps = {
    fuel: number | null;
}

function FuelTelemetry({fuel}: FuelTelemetryProps) {
    return (
        <div>
            <h3>Fuel: {fuel !== null ? `${fuel.toFixed(2)} L` : "N/A"}</h3>
        </div>
    );
}

export default FuelTelemetry;