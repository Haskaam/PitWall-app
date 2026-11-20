type PanelProps = {
    title: string;
    children: React.ReactNode;
};

function Panel({title, children}: PanelProps) {
    return (
        <section className="panel">
            <h2>{title}</h2>
            <div className="panel-content">{children}</div>
        </section>
    );
}

export default Panel;