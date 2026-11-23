using PitWallConnector.Models;

namespace PitWallConnector.Services;

/// <summary>Physical race order; SDK classification remains in Position/ClassPosition.</summary>
public sealed class LiveOrderTracker
{
    private readonly Dictionary<int, double> _previous = new();
    private const double NearTie = 0.00004; // avoid order flicker when cars are almost level

    public void Reset() => _previous.Clear();

    public void Update(List<DriverTelemetry> drivers)
{
    var active = drivers
        .Where(d => d.ClassId is >= 0 && d.Position > 0)
        .ToList();

    bool hasMissingProgress = active.Any(d => !Progress(d).HasValue);

    List<DriverTelemetry> sorted;

    if (hasMissingProgress)
    {
        // Fallback: use official SDK classification.
        sorted = active
            .OrderBy(d => d.Position)
            .ToList();
    }
    else
    {
        // Physical race order based on track progress.
        sorted = active
            .OrderByDescending(d => Progress(d)!.Value)
            .ThenBy(d => d.Position)
            .ToList();

        sorted.Sort((a, b) =>
        {
            double pa = Progress(a)!.Value;
            double pb = Progress(b)!.Value;

            double difference = pb - pa;

            if (Math.Abs(difference) > NearTie)
                return difference > 0 ? 1 : -1;

            return a.Position.CompareTo(b.Position);
        });
    }

    int overall = 1;

    foreach (var car in sorted)
        car.LivePosition = overall++;

    foreach (var group in active.GroupBy(x => x.ClassId))
    {
        int classPos = 1;

        foreach (var car in sorted.Where(x => x.ClassId == group.Key))
            car.LiveClassPosition = classPos++;
    }

    foreach (var car in drivers.Except(active))
    {
        car.LivePosition = car.Position > 0 ? car.Position : 0;
        car.LiveClassPosition = car.ClassPosition > 0 ? car.ClassPosition : 0;
    }
}

    public static double? Progress(DriverTelemetry car)
    {
        if (car.LapCompleted < 0 || car.LapDistPct is not float pct ||
            !float.IsFinite(pct) || pct < 0 || pct >= 1) return null;
        return car.LapCompleted + pct;
    }
}
