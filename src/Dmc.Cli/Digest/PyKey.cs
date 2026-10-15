using System.Numerics;
using Dmc.Maps;

namespace Dmc.Digest;

/// <summary>
/// A JSON value used as a Python dictionary, set or <c>Counter</c> key, which keeps what Python keeps apart and joins what
/// it joins: <c>None</c>, <c>""</c>, <c>1</c> and <c>"1"</c> are four keys, while <c>True</c>, <c>1</c> and <c>1.0</c> are one
/// (and the one first used is the one shown). The value stays, because it is what prints: <c>"%s" % key</c> is
/// <see cref="Display"/>, and <c>key or "?"</c> is <see cref="Truthy"/>.
/// </summary>
internal readonly struct PyKey : IEquatable<PyKey>
{
    readonly string _id;

    public object? Value { get; }

    PyKey(string id, object? value)
    {
        _id = id;
        Value = value;
    }

    /// <summary>The key of a JSON value. A list or an object cannot be one (Python: unhashable type).</summary>
    public static PyKey Of(object? value) => value switch
    {
        null => new PyKey("N", null),
        string s => new PyKey("s:" + s, s),
        bool b => new PyKey(b ? "i:1" : "i:0", value),
        long n => new PyKey("i:" + n, value),
        BigInteger big => new PyKey("i:" + big, value),
        double d when double.IsFinite(d) && d == Math.Floor(d) => new PyKey("i:" + new BigInteger(d), value),
        double d => new PyKey("f:" + Py.FloatRepr(d), value),
        _ => throw new LogShapeException("a list or an object where an id or a name was expected"),
    };

    public static implicit operator PyKey(string text) => Of(text);

    /// <summary>Python's <c>str(key)</c>.</summary>
    public string Display => Py.Str(Value);

    /// <summary>Python's truth value: false for <c>None</c>, <c>""</c>, <c>0</c>, <c>0.0</c> and <c>False</c>.</summary>
    public bool Truthy => Py.Truthy(Value);

    public bool Equals(PyKey other) => _id == other._id;

    public override bool Equals(object? obj) => obj is PyKey other && Equals(other);

    public override int GetHashCode() => (_id ?? "").GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Display;
}
