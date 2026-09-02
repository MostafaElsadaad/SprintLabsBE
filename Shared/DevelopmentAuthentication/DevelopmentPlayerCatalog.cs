namespace Shared.DevelopmentAuthentication;

public static class DevelopmentPlayerCatalog
{
    private static readonly Entry[] Entries = Enumerable.Range(1, 8)
        .Select(number => new Entry(
            $"dev-player-{number:00}",
            $"Dev Player {number:00}",
            $"dev-player-{number:00}@development.sprintlabs.invalid"))
        .ToArray();

    private static readonly IReadOnlyDictionary<string, Entry> ByAccountKey = Entries
        .ToDictionary(entry => entry.AccountKey, StringComparer.Ordinal);

    public static IReadOnlyList<Entry> All { get; } = Array.AsReadOnly(Entries);

    public static bool TryGet(string? accountKey, out Entry entry)
    {
        if (accountKey != null && ByAccountKey.TryGetValue(accountKey, out var match))
        {
            entry = match;
            return true;
        }

        entry = null!;
        return false;
    }

    public sealed record Entry(string AccountKey, string DisplayName, string Email);
}
