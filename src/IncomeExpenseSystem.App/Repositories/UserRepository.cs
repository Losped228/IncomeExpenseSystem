using Dapper;
using IncomeExpenseSystem.App.Database;
using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Repositories;

/// <summary>Репозиторий пользователей на Dapper.</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _factory;

    public UserRepository(IDbConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public User? GetByLogin(string login)
    {
        using var connection = _factory.Create();
        return connection.QuerySingleOrDefault<User>(
            @"SELECT Id, Login, PasswordHash, FullName, Role, IsActive, CreatedAt
              FROM USERS
              WHERE Login = @Login COLLATE NOCASE_UNICODE;",
            new { Login = login });
    }

    public User? GetById(int id)
    {
        using var connection = _factory.Create();
        return connection.QuerySingleOrDefault<User>(
            @"SELECT Id, Login, PasswordHash, FullName, Role, IsActive, CreatedAt
              FROM USERS
              WHERE Id = @Id;",
            new { Id = id });
    }

    public IReadOnlyList<User> GetAll()
    {
        using var connection = _factory.Create();
        return connection.Query<User>(
            @"SELECT Id, Login, PasswordHash, FullName, Role, IsActive, CreatedAt
              FROM USERS
              ORDER BY FullName;").AsList();
    }
}
