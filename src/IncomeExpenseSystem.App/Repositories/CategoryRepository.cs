using Dapper;
using IncomeExpenseSystem.App.Database;
using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Repositories;

/// <summary>Репозиторий категорий на Dapper.</summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _factory;

    public CategoryRepository(IDbConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IReadOnlyList<Category> GetAll(bool includeInactive = false)
    {
        using var connection = _factory.Create();
        var sql = @"SELECT Id, Name, Type, Description, IsActive, CreatedAt
                    FROM CATEGORIES";

        if (!includeInactive)
            sql += " WHERE IsActive = 1";

        sql += " ORDER BY Name;";
        return connection.Query<Category>(sql).AsList();
    }

    public Category? GetById(int id)
    {
        using var connection = _factory.Create();
        return connection.QuerySingleOrDefault<Category>(
            @"SELECT Id, Name, Type, Description, IsActive, CreatedAt
              FROM CATEGORIES
              WHERE Id = @Id;",
            new { Id = id });
    }

    public Category? GetByName(string name)
    {
        using var connection = _factory.Create();
        return connection.QuerySingleOrDefault<Category>(
            @"SELECT Id, Name, Type, Description, IsActive, CreatedAt
              FROM CATEGORIES
              WHERE Name = @Name COLLATE NOCASE_UNICODE;",
            new { Name = name });
    }

    public int Add(Category category)
    {
        using var connection = _factory.Create();
        return connection.ExecuteScalar<int>(
            @"INSERT INTO CATEGORIES(Name, Type, Description, IsActive, CreatedAt)
              VALUES(@Name, @Type, @Description, @IsActive, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                category.Name,
                Type = category.Type.ToString(),
                category.Description,
                category.IsActive,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }

    public bool Update(Category category)
    {
        using var connection = _factory.Create();
        var affected = connection.Execute(
            @"UPDATE CATEGORIES
              SET Name = @Name,
                  Type = @Type,
                  Description = @Description,
                  IsActive = @IsActive
              WHERE Id = @Id;",
            new
            {
                category.Id,
                category.Name,
                Type = category.Type.ToString(),
                category.Description,
                category.IsActive
            });

        return affected > 0;
    }

    public bool Delete(int id)
    {
        using var connection = _factory.Create();
        var affected = connection.Execute(
            "DELETE FROM CATEGORIES WHERE Id = @Id;",
            new { Id = id });

        return affected > 0;
    }

    public bool HasTransactions(int categoryId)
    {
        using var connection = _factory.Create();
        var count = connection.ExecuteScalar<long>(
            "SELECT COUNT(1) FROM TRANSACTIONS WHERE CategoryId = @CategoryId;",
            new { CategoryId = categoryId });

        return count > 0;
    }
}
