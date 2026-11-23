using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Repositories;

/// <summary>Доступ к учётным записям.</summary>
public interface IUserRepository
{
    User? GetByLogin(string login);

    User? GetById(int id);

    IReadOnlyList<User> GetAll();
}
