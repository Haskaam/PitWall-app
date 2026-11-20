type DashboardHeaderProps = {
    title: string;
    children: React.ReactNode;
}

function DashboardHeader({title, children}: DashboardHeaderProps) {
    return (
        <header className="dashboard-header">
            <h1>{title}</h1>
            {children}
        </header>
    );
}

export default DashboardHeader;