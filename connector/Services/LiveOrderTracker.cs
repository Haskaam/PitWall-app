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
        var active = drivers.Where(d => d.ClassId is >= 0 && d.Position > 0).ToList();
        var sorted = active.OrderByDescending(d => Progress(d).HasValue)
            .ThenByDescending(d => Progress(d) ?? double.NegativeInfinity)
            .ThenBy(d => d.Position).ToList();

        // If the difference is extremely small, preserve SDK order as a stable tie breaker.
        sorted.Sort((a, b) =>
        {
            double? pa = Progress(a), pb = Progress(b);
            if (pa.HasValue && pb.HasValue)
            {
                double difference = pb.Value - pa.Value;
                if (Math.Abs(difference) > NearTie) return difference > 0 ? 1 : -1;
            }
            else if (pa.HasValue != pb.HasValue) return pa.HasValue ? -1 : 1;
            return a.Position.CompareTo(b.Position);
        });

        int overall = 1;
        foreach (var car in sorted) car.LivePosition = overall++;
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
