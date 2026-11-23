import type { DashboardTab } from "./DashboardLayout"

type DashboardTabsProps = {
    activeTab: DashboardTab;
    onTabChange: (tab: DashboardTab) => void;
}

function DashboardTabs({activeTab, onTabChange}: DashboardTabsProps) {
    return (
        <div>
            <button
            className={activeTab === "timing" ? "active" : ""}
            onClick={() => onTabChange("timing")}>
                Live Timing
            </button>

            <button
            className={activeTab === "fuel" ? "active" : ""}
            onClick={() => onTabChange("fuel")}>
                Fuel Calculator
            </button>

            <button
            className={activeTab === "fight" ? "active" : ""}
            onClick={() => onTabChange("fight")}>
                Fight Simulation
            </button>
        </div>
    )
}

export default DashboardTabs;