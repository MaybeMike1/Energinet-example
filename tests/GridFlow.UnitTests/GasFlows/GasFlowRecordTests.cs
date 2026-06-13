using GridFlow.Domain.GasFlows;

namespace GridFlow.UnitTests.GasFlows;

public class GasFlowRecordTests
{
    private static readonly GasFlowValues SampleValues = new(
        KWhFromBiogas: 25_312_656,
        KWhToDenmark: -34_839_889,
        KWhFromNorthSea: 207_112_725,
        KWhToOrFromStorage: -5_368_000,
        KWhToOrFromGermany: 106_488,
        KWhToSweden: -11_824_683,
        KWhFromTyra: 40_050_073,
        KWhToPoland: -241_865_040);

    [Fact]
    public void Create_sets_natural_key_and_values()
    {
        var gasDay = new DateOnly(2026, 6, 12);
        var retrievedAt = new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero);

        var record = GasFlowRecord.Create("Gasflow", gasDay, SampleValues, retrievedAt);

        record.Dataset.Should().Be("Gasflow");
        record.GasDay.Should().Be(gasDay);
        record.ToValues().Should().Be(SampleValues);
        record.RetrievedAtUtc.Should().Be(retrievedAt);
    }

    [Fact]
    public void Create_stores_retrieved_timestamp_in_utc()
    {
        var local = new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.FromHours(2));

        var record = GasFlowRecord.Create("Gasflow", new DateOnly(2026, 6, 12), SampleValues, local);

        record.RetrievedAtUtc.Offset.Should().Be(TimeSpan.Zero);
        record.RetrievedAtUtc.Should().Be(local.ToUniversalTime());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_missing_dataset(string dataset)
    {
        var act = () => GasFlowRecord.Create(dataset, new DateOnly(2026, 6, 12), SampleValues, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>().WithParameterName("dataset");
    }

    [Fact]
    public void Create_rejects_default_gas_day()
    {
        var act = () => GasFlowRecord.Create("Gasflow", default, SampleValues, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>().WithParameterName("gasDay");
    }

    [Fact]
    public void Update_overwrites_values_and_timestamp()
    {
        var record = GasFlowRecord.Create(
            "Gasflow",
            new DateOnly(2026, 6, 12),
            SampleValues,
            new DateTimeOffset(2026, 6, 13, 10, 0, 0, TimeSpan.Zero));

        var newValues = SampleValues with { KWhFromBiogas = 99_999 };
        var newTimestamp = new DateTimeOffset(2026, 6, 14, 10, 0, 0, TimeSpan.Zero);

        record.Update(newValues, newTimestamp);

        record.ToValues().Should().Be(newValues);
        record.RetrievedAtUtc.Should().Be(newTimestamp);
    }
}