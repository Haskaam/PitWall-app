import { useState } from "react";
import type { TelemetrySnapshot } from "../types/telemetry";
import DashboardHeader from "./DashboardHeader";
import DashboardTabs from "./DashboardTabs";
import LiveTiming from "./LiveTiming";
import FuelTelemetry from "./FuelTelemetry";

type DashboardLayoutProps = {
    snapshot: TelemetrySnapshot | null;
}

export type DashboardTab = "timing" | "fuel" | "fight";

function DashboardLayout({snapshot}: DashboardLayoutProps) {
    const currentSession = snapshot?.sessionInfo?.sessions.find(
        session => session.sessionNum === snapshot.session.sessionNum
    );

    const [activeTab, setActiveTab] = useState<DashboardTab>("timing");

    return (
        <div className="dashboard">
            <DashboardHeader 
                sessionType={currentSession?.sessionName ?? null}
                timeRemaining={snapshot?.session.sessionTimeRemainSeconds ?? null}
                airTemp={snapshot?.session.airTemperatureC ?? null}
                trackTemp={snapshot?.session.trackTemperatureC ?? null}
                
            />
        <main className="dashboard-main">
            <DashboardTabs 
                activeTab={activeTab}
                onTabChange={setActiveTab}
            />
        <section className="dashboard-panel">
            {activeTab === "timing" && (
                <LiveTiming drivers={snapshot?.drivers ?? []} />
            )}

            {activeTab === "fuel" && (
                <FuelTelemetry
                    fuel={snapshot?.player.fuelLevelLiters ?? null}
                />
            )}

            {activeTab === "fight" && (
                <div>
                    <h2>Fight Simulation</h2>
                    <p>Coming soon...</p>
                </div>
            )}
            </section>
            </main>
        </div>
    )
}

export default DashboardLayout;