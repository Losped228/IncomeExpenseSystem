namespace IncomeExpenseSystem.App.Models;

/// <summary>
/// Финансовая операция (доход или расход).
/// </summary>
public sealed class Transaction
{
    public int Id { get; set; }

    /// <summary>Автор операции.</summary>
    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public DateTime TxDate { get; set; }

    /// <summary>Сумма всегда положительна; тип определяется категорией.</summary>
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
