using Dmc.PersonRecords;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The full-date rule (spec 036 FR-005, data-model.md § Full dates, task T006): an ordinal day needs its month, a range
/// shares the month at its end, and a year is needed only when the campaign's calendar has one.
/// </summary>
[TestClass]
public sealed class RecordDatesTests
{
    [TestMethod]
    public void ADayWithItsMonthIsFull()
    {
        Assert.IsNull(RecordDates.Error("ready on the 14th of Eleasis", needsYear: false));
        Assert.IsNull(RecordDates.Error("hatched on the 12th Nightal, at dusk", needsYear: false));
        Assert.IsNull(RecordDates.Error("no date at all", needsYear: false));
        Assert.IsNull(RecordDates.Error(null, needsYear: false));
    }

    [TestMethod]
    public void ADayAloneIsRefusedWithTheDay()
    {
        Assert.AreEqual("\"the 14th\" has no month: write the full in-world date (the 14th of <month>)", RecordDates.Error("ready on the 14th", needsYear: false));
        Assert.AreEqual("\"9th\" has no month: write the full in-world date (the 9th of <month>)", RecordDates.Error("(s62, 9th)", needsYear: false));
        Assert.IsNotNull(RecordDates.Error("ready on the 14th, about noon", needsYear: false));
    }

    [TestMethod]
    public void ARangeSharesTheMonthAtItsEnd()
    {
        Assert.IsNull(RecordDates.Error("the 8th to the 10th of Flamerule", needsYear: false));
        Assert.IsNull(RecordDates.Error("long rests the nights of the 5th and 6th of Kythorn", needsYear: false));
        Assert.IsNull(RecordDates.Error("the 1st-3rd of Ches", needsYear: false));
        Assert.IsNotNull(RecordDates.Error("the 8th to the 10th", needsYear: false));
        // T034: a word before the joint, and a month counted in words (the false hits of SC-002's sample).
        Assert.IsNull(RecordDates.Error("the 14th night to the 18th of Eleasis", needsYear: false));
        Assert.IsNull(RecordDates.Error("met the wagon (the 5th of the second month)", needsYear: false));
        Assert.IsNotNull(RecordDates.Error("the 14th night", needsYear: false));
    }

    [TestMethod]
    public void AYearIsNeededOnlyWhenTheCalendarHasOne()
    {
        Assert.IsNull(RecordDates.Error("the 14th of Eleasis, 1492 DR", needsYear: true));
        Assert.IsNull(RecordDates.Error("the 14th of Eleasis in the year 1492", needsYear: true));
        Assert.AreEqual("\"the 14th of Eleasis\" has no year: write the full in-world date with its year", RecordDates.Error("the 14th of Eleasis", needsYear: true));
        Assert.IsNull(RecordDates.Error("the 14th of Eleasis", needsYear: false));
    }

    [TestMethod]
    public void AnOrdinalThatIsNotADayIsReadAsOne()
    {
        // dmc has no calendar: a capitalised word after an ordinal reads as a month, and a lower-case one does not.
        Assert.IsNull(RecordDates.Error("the 58th Company", needsYear: false));
        Assert.IsNotNull(RecordDates.Error("the 3rd floor", needsYear: false));
    }

    [TestMethod]
    public void TheCalendarHasAYearWhenTheGameStatesDateHoldsOne()
    {
        string folder = Path.Combine(Path.GetTempPath(), "dmc-dates-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Assert.IsFalse(RecordDates.CalendarHasYear(folder), "no game-state.md");
            File.WriteAllText(Path.Combine(folder, "game-state.md"), "## Scene\n\n- **In-world date and time**: Wednesday the 18th of Eleasis, about 6 AM\n");
            Assert.IsFalse(RecordDates.CalendarHasYear(folder));
            File.WriteAllText(Path.Combine(folder, "game-state.md"), "## Scene\n\n- **In-world date and time**: the 18th of Eleasis, 1492 DR <!-- a note 2026 -->\n");
            Assert.IsTrue(RecordDates.CalendarHasYear(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
