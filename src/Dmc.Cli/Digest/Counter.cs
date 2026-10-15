namespace Dmc.Digest;

/// <summary>
/// Python's <c>collections.Counter</c> of text keys, as <c>transcript_digest.py</c> uses it: a count that is 0 for a
/// key never added, keys kept in the order they were first added, and <see cref="MostCommon"/> putting equal counts in
/// that order (as <c>Counter.most_common</c> does). A JSON <c>null</c> key is the empty string.
/// </summary>
internal sealed class Counter
{
    readonly OrderedDictionary<string, long> _counts = [];

    public long this[string key] => _counts.TryGetValue(key, out long n) ? n : 0;

    public int Count => _counts.Count;

    public bool IsEmpty => _counts.Count == 0;

    public IEnumerable<KeyValuePair<string, long>> Items => _counts;

    public long Total => _counts.Values.Sum();

    public void Add(string key, long amount = 1) => _counts[key] = this[key] + amount;

    /// <summary>The <paramref name="n"/> largest counts, biggest first, equal counts in the order first added.</summary>
    public List<KeyValuePair<string, long>> MostCommon(int n) =>
        _counts.OrderByDescending(p => p.Value).Take(n).ToList();

    /// <summary>The counts as a plain dictionary, for a test to compare.</summary>
    public Dictionary<string, long> ToDictionary() => new(_counts);
}
