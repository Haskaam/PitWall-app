import type { DashboardTab } from "./DashboardLayout"

type DashboardTabsProps = {
    activeTab: DashboardTab;
    onTabChange: (tab: DashboardTab) => void;
}

function DashboardTabs({activeTab, onTabChange}: DashboardTabsProps) {
    return (
        <nav className="dashboard-tabs">
            <button
            className={activeTab === "timing" ? "tab active" : "tab"}
            onClick={() => onTabChange("timing")}>
                Live Timing
            </button>

            <button
            className={activeTab === "fuel" ? "tab active" : "tab"}
            onClick={() => onTabChange("fuel")}>
                Fuel Calculator
            </button>

            <button
            className={activeTab === "fight" ? "tab active" : "tab"}
            onClick={() => onTabChange("fight")}>
                Fight Simulation
            </button>
        </nav>
    )
}

export default DashboardTabs;