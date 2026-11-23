using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Repositories;

/// <summary>Доступ к финансовым операциям.</summary>
public interface ITransactionRepository
{
    IReadOnlyList<TransactionView> GetFiltered(TransactionFilter filter);

    Transaction? GetById(int id);

    int Add(Transaction transaction);

    bool Update(Transaction transaction);

    bool Delete(int id);

    int CountByCategory(int categoryId);

    IReadOnlyList<decimal> GetAmounts(TransactionFilter filter);

    IReadOnlyList<MonthlyTotal> GetMonthlyTotals(DateTime from, DateTime to);

    IReadOnlyList<CategoryTotal> GetCategoryTotals(DateTime from, DateTime to, CategoryType? type = null);
}
