using Zmg.Domain.Entities;
using Zmg.Domain.Enums;

namespace Zmg.Domain;

public readonly record struct ProgressCount(int Done, int Total)
{
    public double Fraction => Total == 0 ? 0 : (double)Done / Total;
    public int Percent => Total == 0 ? 0 : (int)Math.Round(Fraction * 100);
}

public readonly record struct ReleaseProgress(ProgressCount Overall, IReadOnlyDictionary<Phase, ProgressCount> ByPhase);

/// <summary>
/// Pure progress calculation over a release's task list. Computed in Domain so the
/// list endpoint can return counts without the frontend re-fetching every task.
/// </summary>
public static class ProgressCalculator
{
    public static ProgressCount Count(IEnumerable<ReleaseTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        int done = 0, total = 0;
        foreach (var t in tasks)
        {
            total++;
            if (t.IsDone) done++;
        }
        return new ProgressCount(done, total);
    }

    public static ReleaseProgress Calculate(IEnumerable<ReleaseTask> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        var list = tasks as ICollection<ReleaseTask> ?? tasks.ToList();

        var byPhase = new Dictionary<Phase, ProgressCount>();
        foreach (Phase phase in Enum.GetValues<Phase>())
        {
            byPhase[phase] = Count(list.Where(t => t.Phase == phase));
        }

        return new ReleaseProgress(Count(list), byPhase);
    }
}
