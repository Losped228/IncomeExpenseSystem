namespace IncomeExpenseSystem.App.Models;

/// <summary>
/// Представление операции для таблиц и отчётов (результат JOIN).
/// </summary>
public sealed class TransactionView
{
    public int Id { get; set; }

    public DateTime TxDate { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public CategoryType CategoryType { get; set; }

    public decimal Amount { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public string TypeDisplayName => CategoryType == CategoryType.Income ? "Доход" : "Расход";
}

/// <summary>
/// Параметры фильтрации выборки операций.
/// </summary>
public sealed class TransactionFilter
{
    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public int? CategoryId { get; init; }

    public CategoryType? Type { get; init; }

    public string? Search { get; init; }

    public int Limit { get; init; } = 500;

    public int Offset { get; init; }
}

/// <summary>
/// Агрегат по месяцу и типу операции.
/// </summary>
public sealed class MonthlyTotal
{
    /// <summary>Месяц в формате yyyy-MM.</summary>
    public string Month { get; set; } = string.Empty;

    public CategoryType Type { get; set; }

    public decimal Amount { get; set; }
}

/// <summary>
/// Агрегат по категории.
/// </summary>
public sealed class CategoryTotal
{
    public string CategoryName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}

/// <summary>
/// Результат расчёта описательных статистик.
/// </summary>
public sealed class StatisticsResult
{
    public int Count { get; init; }

    public decimal Sum { get; init; }

    public decimal Mean { get; init; }

    public decimal Median { get; init; }

    public decimal Variance { get; init; }

    public decimal StandardDeviation { get; init; }

    public decimal Min { get; init; }

    public decimal Max { get; init; }

    public decimal CoefficientOfVariation { get; init; }
}

/// <summary>
/// Результат попытки входа в систему.
/// </summary>
public sealed class AuthResult
{
    public bool Success { get; init; }

    public User? User { get; init; }

    public string Message { get; init; } = string.Empty;

    public static AuthResult Ok(User user) => new()
    {
        Success = true,
        User = user,
        Message = "Вход выполнен."
    };

    public static AuthResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// Данные отчёта «Транзакции за период».
/// </summary>
public sealed class ReportData
{
    public DateTime From { get; init; }

    public DateTime To { get; init; }

    public string GeneratedBy { get; init; } = string.Empty;

    public DateTime GeneratedAt { get; init; }

    public IReadOnlyList<TransactionView> Rows { get; init; } = Array.Empty<TransactionView>();

    public decimal TotalIncome { get; init; }

    public decimal TotalExpense { get; init; }

    public decimal Balance => TotalIncome - TotalExpense;
}
