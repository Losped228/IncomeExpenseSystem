using IncomeExpenseSystem.App.Database;
using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;

namespace IncomeExpenseSystem.App.Services;

/// <summary>
/// Точка сборки зависимостей приложения. Упрощает создание форм
/// и позволяет подменить базу данных в тестах.
/// </summary>
public sealed class ServiceContainer
{
    public IUserRepository Users { get; }

    public ICategoryRepository Categories { get; }

    public ITransactionRepository Transactions { get; }

    public AuthService Auth { get; }

    public StatisticsService Statistics { get; }

    public ReportService Reports { get; }

    public ServiceContainer(IDbConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        Users = new UserRepository(factory);
        Categories = new CategoryRepository(factory);
        Transactions = new TransactionRepository(factory);
        Auth = new AuthService(Users);
        Statistics = new StatisticsService();
        Reports = new ReportService(Transactions);
    }
}
