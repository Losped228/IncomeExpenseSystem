using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Services;

/// <summary>
/// Описательные статистики по выборке денежных сумм.
///
/// Среднее арифметическое:
///   x̄ = (1/n) · Σ xᵢ
///
/// Выборочная дисперсия (несмещённая оценка):
///   D = (1/(n−1)) · Σ (xᵢ − x̄)²
///
/// Среднеквадратическое отклонение:
///   σ = √D
///
/// Медиана:
///   для нечётного n — центральный элемент упорядоченной выборки;
///   для чётного n — среднее двух центральных элементов.
///
/// Коэффициент вариации:
///   CV = (σ / x̄) · 100 %.
/// </summary>
public sealed class StatisticsService
{
    /// <summary>Среднее арифметическое значение.</summary>
    public decimal CalculateMean(IReadOnlyCollection<decimal> values)
    {
        EnsureNotEmpty(values);
        return values.Sum() / values.Count;
    }

    /// <summary>Выборочная дисперсия (делитель n − 1).</summary>
    public decimal CalculateVariance(IReadOnlyCollection<decimal> values)
    {
        EnsureSample(values);
        var mean = CalculateMean(values);
        var sumOfSquares = values.Sum(x => (x - mean) * (x - mean));
        return sumOfSquares / (values.Count - 1);
    }

    /// <summary>Медиана упорядоченной выборки.</summary>
    public decimal CalculateMedian(IReadOnlyCollection<decimal> values)
    {
        EnsureNotEmpty(values);

        var sorted = values.OrderBy(x => x).ToArray();
        var middle = sorted.Length / 2;

        return sorted.Length % 2 == 0
            ? (sorted[middle - 1] + sorted[middle]) / 2m
            : sorted[middle];
    }

    /// <summary>Среднеквадратическое отклонение.</summary>
    public decimal CalculateStandardDeviation(IReadOnlyCollection<decimal> values)
    {
        var variance = CalculateVariance(values);
        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>Коэффициент вариации в процентах.</summary>
    public decimal CalculateCoefficientOfVariation(IReadOnlyCollection<decimal> values)
    {
        var mean = CalculateMean(values);
        if (mean == 0m)
            return 0m;

        var deviation = CalculateStandardDeviation(values);
        return deviation / mean * 100m;
    }

    /// <summary>
    /// Полный набор статистик для интерфейса. Если в выборке одна операция,
    /// дисперсия и стандартное отклонение принимаются равными нулю.
    /// </summary>
    public StatisticsResult Calculate(IReadOnlyCollection<decimal> values)
    {
        EnsureNotEmpty(values);

        var mean = CalculateMean(values);
        var median = CalculateMedian(values);
        var variance = values.Count > 1 ? CalculateVariance(values) : 0m;
        var deviation = values.Count > 1 ? (decimal)Math.Sqrt((double)variance) : 0m;
        var coefficient = mean == 0m ? 0m : deviation / mean * 100m;

        return new StatisticsResult
        {
            Count = values.Count,
            Sum = values.Sum(),
            Mean = mean,
            Median = median,
            Variance = variance,
            StandardDeviation = deviation,
            Min = values.Min(),
            Max = values.Max(),
            CoefficientOfVariation = coefficient
        };
    }

    private static void EnsureNotEmpty(IReadOnlyCollection<decimal> values)
    {
        if (values is null)
            throw new ArgumentNullException(nameof(values));

        if (values.Count == 0)
            throw new ArgumentException("Выборка не должна быть пустой.", nameof(values));
    }

    private static void EnsureSample(IReadOnlyCollection<decimal> values)
    {
        EnsureNotEmpty(values);

        if (values.Count < 2)
            throw new ArgumentException("Для выборочной дисперсии нужно не менее двух значений.", nameof(values));
    }
}
