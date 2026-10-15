using System.Numerics;
using Dmc.Maps;

namespace Dmc.Tests.Map;

/// <summary>
/// Tests of the Python behaviour that <c>dmc map</c> copies so that its files and messages are identical to the
/// script's. The values were checked against Python 3.11 (<c>f"{x:.1f}"</c>, <c>repr</c>, <c>json.loads</c>).
/// </summary>
[TestClass]
public sealed class PythonCompatTests
{
    [TestMethod]
    public void FixedRoundsTheExactValueHalfToEven()
    {
        Assert.AreEqual("0.2", Py.F1(0.25));
        Assert.AreEqual("0.8", Py.F1(0.75));
        Assert.AreEqual("1.2", Py.F1(1.25));
        Assert.AreEqual("0.1", Py.F1(0.05)); // 0.05 is a little above a tie in binary
        Assert.AreEqual("0.1", Py.F1(0.1));
        Assert.AreEqual("-0.0", Py.F1(-0.04));
        Assert.AreEqual("2", Py.F0(2.5));
        Assert.AreEqual("4", Py.F0(3.5));
        Assert.AreEqual("-0", Py.F0(-0.5));
        Assert.AreEqual("498310244715575.2", Py.F1(498310244715575.25)); // past the 15th digit
        Assert.AreEqual("942", Py.F0(942));
        Assert.AreEqual("16.8", Py.F1(40 * 0.42));
    }

    [TestMethod]
    public void FloatReprIsTheShortestRoundTrip()
    {
        Assert.AreEqual("20.0", Py.FloatRepr(20));
        Assert.AreEqual("16.8", Py.FloatRepr(16.8));
        Assert.AreEqual("9.600000000000001", Py.FloatRepr(24 * 0.4));
        Assert.AreEqual("0.045", Py.FloatRepr(0.045));
        Assert.AreEqual("1e+16", Py.FloatRepr(1e16));
        Assert.AreEqual("1e-05", Py.FloatRepr(1e-5));
        Assert.AreEqual("123456789.12345679", Py.FloatRepr(123456789.123456789));
        Assert.AreEqual("-0.0", Py.FloatRepr(-0.0));
        Assert.AreEqual("inf", Py.FloatRepr(double.PositiveInfinity));
    }

    [TestMethod]
    public void GFormatsAWholeNumberLikePython()
    {
        Assert.AreEqual("10000", Py.G(10000));
        Assert.AreEqual("999999", Py.G(999999));
        Assert.AreEqual("1e+06", Py.G(1000000));
        Assert.AreEqual("1.23457e+06", Py.G(1234567));
    }

    [TestMethod]
    public void StrAndReprOfValuesFromAFile()
    {
        Assert.AreEqual("None", Py.Str(null));
        Assert.AreEqual("True", Py.Str(true));
        Assert.AreEqual("2.5", Py.Str(2.5));
        Assert.AreEqual("[1, 'x', None, \"it's\"]", Py.Str(MapFixtures.Arr(1, "x", null, "it's")));
        Assert.AreEqual("{'a': 1}", Py.Str(MapFixtures.Obj(("a", 1))));
        Assert.AreEqual("'\\n\\\\'", Py.Repr("\n\\"));
    }

    [TestMethod]
    public void JsonReadsWhatPythonReads()
    {
        Assert.AreEqual(double.PositiveInfinity, PyJson.Parse("Infinity"));
        Assert.IsTrue(double.IsNaN((double)PyJson.Parse("NaN")!));
        Assert.AreEqual(BigInteger.Parse("12345678901234567890123"), PyJson.Parse("12345678901234567890123"));
        Assert.AreEqual(0L, PyJson.Parse("-0"));
        Assert.AreEqual(5.0, PyJson.Parse("5.0e0"));
        var dict = (PyDict)PyJson.Parse("""{"a": 1, "b": 2, "a": 3}""")!;
        CollectionAssert.AreEqual(new[] { "a", "b" }, dict.Keys.ToArray()); // a later duplicate keeps the first place
        Assert.AreEqual(3L, dict["a"]);
        Assert.AreEqual("\ud83d\ude00", PyJson.Parse("\"\\ud83d\\ude00\""));
    }

    [TestMethod]
    public void JsonMistakesAreWordedLikePython()
    {
        var cases = new Dictionary<string, string>
        {
            [""] = "Expecting value: line 1 column 1 (char 0)",
            ["{"] = "Expecting property name enclosed in double quotes: line 1 column 2 (char 1)",
            ["["] = "Expecting value: line 1 column 2 (char 1)",
            ["{\"a\" 1}"] = "Expecting ':' delimiter: line 1 column 6 (char 5)",
            ["[1 2]"] = "Expecting ',' delimiter: line 1 column 4 (char 3)",
            ["[1,]"] = "Expecting value: line 1 column 4 (char 3)",
            ["{\"a\":1,}"] = "Expecting property name enclosed in double quotes: line 1 column 8 (char 7)",
            ["{} x"] = "Extra data: line 1 column 4 (char 3)",
            ["\"abc"] = "Unterminated string starting at: line 1 column 1 (char 0)",
            ["\"\\x\""] = "Invalid \\escape: line 1 column 2 (char 1)",
            ["\"\\u12G4\""] = "Invalid \\uXXXX escape: line 1 column 3 (char 2)",
            ["\"a\nb\""] = "Invalid control character at: line 1 column 3 (char 2)",
            ["\ufeff{}"] = "Unexpected UTF-8 BOM (decode using utf-8-sig): line 1 column 1 (char 0)",
            ["\n\n  [1,\n 2 3]"] = "Expecting ',' delimiter: line 4 column 4 (char 11)",
        };
        foreach (var (text, message) in cases)
        {
            var caught = Assert.ThrowsExactly<JsonDecodeError>(() => PyJson.Parse(text), text);
            Assert.AreEqual(message, caught.Message, text);
        }
    }

    [TestMethod]
    public void JsonIsWrittenAsPythonWritesIt()
    {
        var value = MapFixtures.Obj(("a", MapFixtures.Arr(1, 2.5, null, true)), ("b", "caf\u00e9 \ud83d\ude00 \"q\" \\ \u007f"), ("c", MapFixtures.Obj()));
        Assert.AreEqual("{\"a\": [1, 2.5, null, true], \"b\": \"caf\\u00e9 \\ud83d\\ude00 \\\"q\\\" \\\\ \\u007f\", \"c\": {}}", PyJson.Dumps(value));
        Assert.AreEqual("Infinity", PyJson.Dumps(double.PositiveInfinity));
        Assert.AreEqual("[]", PyJson.Dumps(MapFixtures.Arr()));
    }

    [TestMethod]
    public void TextIsSplitAndTrimmedLikePython()
    {
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, Py.Split(" a\u00a0b\t\nc ").ToArray());
        Assert.AreEqual("x y", Py.Strip("\u3000 x y \n"));
        Assert.AreEqual(2, Py.Len("a\ud83d\ude00"));
    }
}
