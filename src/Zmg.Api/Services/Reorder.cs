namespace Zmg.Api.Services;

/// <summary>
/// Shared reorder mechanic used by tracks, release tasks and template tasks: the request must
/// list every entity in the group exactly once. On success, applies the new position to each
/// entity via <paramref name="setPosition"/> (0-based index; callers adapt, e.g. tracks use i+1).
/// </summary>
internal static class Reorder
{
    public static bool TryApply<T>(
        IReadOnlyList<T> entities,
        IReadOnlyList<Guid> orderedIds,
        Func<T, Guid> idOf,
        Action<T, int> setPosition)
    {
        var byId = entities.ToDictionary(idOf);
        if (orderedIds.Count != entities.Count || orderedIds.Any(id => !byId.ContainsKey(id)))
            return false;

        for (var i = 0; i < orderedIds.Count; i++)
            setPosition(byId[orderedIds[i]], i);
        return true;
    }
}
