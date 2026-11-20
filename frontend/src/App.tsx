import "./App.css";
import Panel from "./components/Panel";
import DashboardHeader from "./components/DashboardHeader";
import FuelTelemetry from "./components/FuelTelemetry";
import LiveTiming from "./components/LiveTiming";
import { useTelemetry } from "./hooks/useTelemetry";


function App() {
  const {telemetry, error} = useTelemetry();
  const snapshot = telemetry?.snapshot;

  return (
    <div className="dashboard">
      <DashboardHeader title="PITWALL">
        <span>iRacing Race Engineer</span>
      </DashboardHeader>

      <main className="dashboard-content">
        <Panel title="Live Timing">
          <LiveTiming drivers={snapshot?.drivers ?? []} />
        </Panel>

        <Panel title="Fuel Calculator">
          <FuelTelemetry
            fuel={snapshot?.player.fuelLevelLiters ?? null}
            connected={telemetry?.fresh === true && snapshot?.connected === true}
          />
        </Panel>

        <Panel title="Session Information">
          <p>Air: {snapshot?.session.airTemperatureC.toFixed(1) ?? "N/A"} °C</p>
          <p>Track: {snapshot?.session.trackTemperatureC.toFixed(1) ?? "N/A"} °C</p>
          {error && <p>API ERROR: {error}</p>}
        </Panel>
      </main>
    </div>
  );
}

export default App;