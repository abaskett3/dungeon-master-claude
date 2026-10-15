namespace Dmc.Digest;

/// <summary>
/// Python's <c>collections.Counter</c>, as <c>transcript_digest.py</c> uses it: a count that is 0 for a key never added,
/// keys kept in the order they were first added, and <see cref="MostCommon"/> putting equal counts in that order (as
/// <c>Counter.most_common</c> does). Keys are <see cref="PyKey"/>s: a text converts to one, and a JSON <c>null</c> is a
/// key of its own.
/// </summary>
internal sealed class Counter
{
    readonly OrderedDictionary<PyKey, (PyKey Key, long Count)> _counts = [];

    public long this[PyKey key] => _counts.TryGetValue(key, out var entry) ? entry.Count : 0;

    public int Count => _counts.Count;

    public bool IsEmpty => _counts.Count == 0;

    /// <summary>The keys as first used, with their counts.</summary>
    public IEnumerable<KeyValuePair<PyKey, long>> Items => _counts.Values.Select(e => KeyValuePair.Create(e.Key, e.Count));

    public void Add(PyKey key, long amount = 1) =>
        _counts[key] = (_counts.TryGetValue(key, out var entry) ? entry.Key : key, this[key] + amount);

    /// <summary>The <paramref name="n"/> largest counts, biggest first, equal counts in the order first added.</summary>
    public List<KeyValuePair<PyKey, long>> MostCommon(int n) => Items.OrderByDescending(p => p.Value).Take(n).ToList();

    /// <summary>The counts by the text of their keys, for a test to compare.</summary>
    public Dictionary<string, long> ToDictionary() => Items.ToDictionary(p => p.Key.Display, p => p.Value);
}
