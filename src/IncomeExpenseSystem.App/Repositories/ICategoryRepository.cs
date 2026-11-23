using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Repositories;

/// <summary>Доступ к справочнику категорий.</summary>
public interface ICategoryRepository
{
    IReadOnlyList<Category> GetAll(bool includeInactive = false);

    Category? GetById(int id);

    Category? GetByName(string name);

    int Add(Category category);

    bool Update(Category category);

    bool Delete(int id);

    bool HasTransactions(int categoryId);
}
