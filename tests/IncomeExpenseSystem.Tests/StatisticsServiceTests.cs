using FluentAssertions;
using IncomeExpenseSystem.App.Services;
using Xunit;

namespace IncomeExpenseSystem.Tests;

public sealed class StatisticsServiceTests
{
    private readonly StatisticsService _service = new();

    [Fact]
    public void CalculateMean_ReturnsArithmeticMean()
    {
        var values = new decimal[] { 10, 12, 14, 16, 18 };

        _service.CalculateMean(values).Should().Be(14m);
    }

    [Fact]
    public void CalculateVariance_ReturnsSampleVariance()
    {
        var values = new decimal[] { 10, 12, 14, 16, 18 };

        _service.CalculateVariance(values).Should().Be(10m);
    }

    [Fact]
    public void CalculateMedian_ReturnsMiddleValueForOddCount()
    {
        var values = new decimal[] { 18, 10, 16, 14, 12 };

        _service.CalculateMedian(values).Should().Be(14m);
    }

    [Fact]
    public void CalculateMedian_ReturnsAverageOfTwoMiddleValuesForEvenCount()
    {
        var values = new decimal[] { 1, 2, 3, 4 };

        _service.CalculateMedian(values).Should().Be(2.5m);
    }

    [Fact]
    public void CalculateStandardDeviation_ReturnsSquareRootOfVariance()
    {
        var values = new decimal[] { 10, 12, 14, 16, 18 };

        _service.CalculateStandardDeviation(values)
            .Should().BeApproximately(3.1623m, 0.0001m);
    }

    [Fact]
    public void Calculate_ReturnsCompleteStatistics()
    {
        var values = new decimal[] { 10, 12, 14, 16, 18 };

        var result = _service.Calculate(values);

        result.Count.Should().Be(5);
        result.Sum.Should().Be(70m);
        result.Mean.Should().Be(14m);
        result.Median.Should().Be(14m);
        result.Variance.Should().Be(10m);
        result.StandardDeviation.Should().BeApproximately(3.1623m, 0.0001m);
        result.Min.Should().Be(10m);
        result.Max.Should().Be(18m);
        result.CoefficientOfVariation.Should().BeApproximately(22.587m, 0.01m);
    }

    [Fact]
    public void Calculate_WithOneValue_ReturnsZeroDispersion()
    {
        var result = _service.Calculate(new decimal[] { 42m });

        result.Count.Should().Be(1);
        result.Variance.Should().Be(0m);
        result.StandardDeviation.Should().Be(0m);
        result.CoefficientOfVariation.Should().Be(0m);
    }

    [Fact]
    public void CalculateMean_WithEmptySample_Throws()
    {
        var action = () => _service.CalculateMean(Array.Empty<decimal>());

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateVariance_WithOneValue_Throws()
    {
        var action = () => _service.CalculateVariance(new decimal[] { 42m });

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculateMedian_WithEmptySample_Throws()
    {
        var action = () => _service.CalculateMedian(Array.Empty<decimal>());

        action.Should().Throw<ArgumentException>();
    }
}
