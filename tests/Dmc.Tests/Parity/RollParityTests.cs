using Dmc.Tests.Support;

namespace Dmc.Tests.Parity;

/// <summary>
/// <c>dmc roll</c> against <c>scripts/roll.sh</c> and <c>scripts/roll.ps1</c> on the same expressions. Dice values
/// differ from run to run, so a rolled line is compared by format and range (see <see cref="RollOutput"/>);
/// everything else, error lines and exit codes included, must be identical.
/// </summary>
[TestClass]
public sealed class RollParityTests
{
    static readonly ParityTool RollSh = new("roll.sh", "roll", (work, args, stdin) =>
    {
        string? bash = Legacy.Bash();
        TestEnv.Need(bash is not null, "bash");
        return TestEnv.Run(bash!, [Legacy.Script("roll.sh"), .. args], stdin, work);
    }, RollOutput.Normalize);

    static readonly ParityTool RollPs1 = new("roll.ps1", "roll", (work, args, stdin) =>
    {
        string? shell = Legacy.PowerShell();
        TestEnv.Need(shell is not null, "PowerShell");
        return TestEnv.Run(shell!, ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Legacy.Script("roll.ps1"), .. args],
            stdin, work);
    }, RollOutput.Normalize);

    static ParityCase[] Cases { get; } =
    [
        // Valid expressions.
        new("single die with modifier", "1d20+5"),
        new("count defaults to one", "d6"),
        new("negative modifier", "3d6-2"),
        new("no modifier", "2d8"),
        new("keep highest", "4d6kh3"),
        new("advantage and disadvantage", "2d20kh1+5", "2d20kl1+5", "2d20kh1-1", "2d20kl1"),
        new("upper case", "2D20KH1"),
        new("leading zeros", "02d06", "1d020", "1d20+05"),
        new("keep every die", "3d6kh3", "3d6kl3"),
        new("the largest allowed", "100d1000", "100d2", "1d1000-5"),
        new("several in order", "1d20+5", "2d6+3", "4d6kh3"),
        // Invalid ones: the error line, the exit code and the dice still rolled around it.
        new("no arguments"),
        new("not dice", "banana"),
        new("a number and no die", "1d"),
        new("a die and nothing else", "d"),
        new("a dangling sign", "1d20+"),
        new("a dangling keep", "1d20kh"),
        new("two terms", "2d6+1d4"),
        new("a space inside", "1d20 +5"),
        new("an out of range count", "101d6", "0d6"),
        new("an out of range die", "1d1", "1d1001"),
        new("keeping more than rolled", "2d20kh3", "2d20kl0"),
        new("a bad one between good ones", "1d20", "banana", "1d6"),
        new("every kind of error in one call", "banana", "101d6", "2d20kh3", "1d6"),
        new("upper case error is echoed as typed", "BANANA", "101D6"),
    ];

    public static IEnumerable<object[]> CaseData => Cases.Select(c => new object[] { c });

    public static string CaseName(System.Reflection.MethodInfo method, object[] data) => $"{method.Name}: {data[0]}";

    [TestMethod]
    [DynamicData(nameof(CaseData), DynamicDataDisplayName = nameof(CaseName))]
    public void RollMatchesRollSh(ParityCase test) => ParityCheck.Run(RollSh, test);

    [TestMethod]
    [DynamicData(nameof(CaseData), DynamicDataDisplayName = nameof(CaseName))]
    public void RollMatchesRollPs1(ParityCase test) => ParityCheck.Run(RollPs1, test);
}
