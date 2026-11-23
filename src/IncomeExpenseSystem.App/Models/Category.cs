namespace IncomeExpenseSystem.App.Models;

/// <summary>
/// Тип статьи учёта.
/// </summary>
public enum CategoryType
{
    Income,
    Expense
}

/// <summary>
/// Статья учёта доходов или расходов.
/// </summary>
public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CategoryType Type { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public string TypeDisplayName => Type == CategoryType.Income ? "Доход" : "Расход";

    public string ActiveDisplay => IsActive ? "Да" : "Нет";
}
