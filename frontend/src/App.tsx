import "./App.css";
import DashboardLayout from "./components/DashboardLayout";
import { useTelemetry } from "./hooks/useTelemetry";


function App() {
  const {telemetry} = useTelemetry();
  const snapshot = telemetry?.snapshot;

  return (
    <div className="dashboard">
      <DashboardLayout 
        snapshot={snapshot ?? null}
      />
    </div>
  );
}

export default App;