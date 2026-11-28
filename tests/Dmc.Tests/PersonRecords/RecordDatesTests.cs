using Dmc.PersonRecords;

namespace Dmc.Tests.PersonRecords;

/// <summary>
/// The full-date rule (spec 036 FR-005, data-model.md § Full dates, task T006): an ordinal day needs a Harptos month and a
/// year, a range shares the month and year at its end, and an ordinal followed straight by a name is not a date.
/// </summary>
[TestClass]
public sealed class RecordDatesTests
{
    [TestMethod]
    public void ADayWithItsMonthAndYearIsFull()
    {
        Assert.IsNull(RecordDates.Error("ready on the 14th of Eleasis, 1493 DR"));
        Assert.IsNull(RecordDates.Error("hatched on the 12th Nightal 1492, at dusk"));
        Assert.IsNull(RecordDates.Error("the 14th of Eleasis in the year 1492"));
        Assert.IsNull(RecordDates.Error("no date at all"));
        Assert.IsNull(RecordDates.Error(null));
    }

    [TestMethod]
    public void ADayAloneIsRefusedWithTheDay()
    {
        Assert.AreEqual("\"the 14th\" has no month: write the full in-world date (the 14th of <month>, <year> DR)", RecordDates.Error("ready on the 14th"));
        Assert.AreEqual("\"9th\" has no month: write the full in-world date (the 9th of <month>, <year> DR)", RecordDates.Error("(s62, 9th)"));
        Assert.IsNotNull(RecordDates.Error("ready on the 14th, about noon"));
        Assert.IsNotNull(RecordDates.Error("the 26th, Tam said"));
    }

    [TestMethod]
    public void ADateNeedsItsYear()
    {
        Assert.AreEqual("\"the 14th of Eleasis\" has no year: write the full in-world date with its year (<year> DR)", RecordDates.Error("the 14th of Eleasis"));
        Assert.IsNotNull(RecordDates.Error("hatched on the 12th Nightal, at dusk"));
    }

    [TestMethod]
    public void AMonthOutsideTheHarptosCalendarIsRefused()
    {
        Assert.AreEqual("\"the 3rd of Harvestmoon\": Harvestmoon is not a month of the Harptos calendar (Hammer, Alturiak, Ches, Tarsakh, Mirtul, Kythorn, Flamerule, Eleasis, Eleint, Marpenoth, Uktar, Nightal)",
            RecordDates.Error("the 3rd of Harvestmoon, 1492 DR"));
    }

    [TestMethod]
    public void ARangeSharesTheMonthAndYearAtItsEnd()
    {
        Assert.IsNull(RecordDates.Error("the 8th to the 10th of Flamerule, 1493 DR"));
        Assert.IsNull(RecordDates.Error("long rests the nights of the 5th and 6th of Kythorn 1493"));
        Assert.IsNull(RecordDates.Error("the 1st-3rd of Ches, 1493 DR"));
        Assert.IsNotNull(RecordDates.Error("the 8th to the 10th"));
        Assert.IsNotNull(RecordDates.Error("the 8th to the 10th of Flamerule"));
        // T034: a word before the joint, and a month counted in words (the false hits of SC-002's sample).
        Assert.IsNull(RecordDates.Error("the 14th night to the 18th of Eleasis, 1493 DR"));
        Assert.IsNull(RecordDates.Error("met the wagon (the 5th of the second month, 1493 DR)"));
        Assert.IsNotNull(RecordDates.Error("the 14th night"));
    }

    [TestMethod]
    public void AnOrdinalBeforeANameIsNotADate()
    {
        Assert.IsNull(RecordDates.Error("the 58th Dwarven Company"));
        Assert.IsNull(RecordDates.Error("a 1st Class ticket"));
        // A lower-case word after an ordinal still reads as a day alone: dmc cannot tell a floor from a date.
        Assert.IsNotNull(RecordDates.Error("the 3rd floor"));
    }
}
