using Dapper;
using IncomeExpenseSystem.App.Database;
using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Repositories;

/// <summary>Репозиторий операций на Dapper.</summary>
public sealed class TransactionRepository : ITransactionRepository
{
    private const string FilterSql = @"
        WHERE (@From IS NULL OR date(t.TxDate) >= date(@From))
          AND (@To IS NULL OR date(t.TxDate) <= date(@To))
          AND (@CategoryId IS NULL OR t.CategoryId = @CategoryId)
          AND (@Type IS NULL OR c.Type = @Type)
          AND (@Search IS NULL
               OR t.Comment LIKE '%' || @Search || '%'
               OR c.Name LIKE '%' || @Search || '%')";

    private readonly IDbConnectionFactory _factory;

    public TransactionRepository(IDbConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IReadOnlyList<TransactionView> GetFiltered(TransactionFilter filter)
    {
        using var connection = _factory.Create();
        var sql = @"
            SELECT t.Id,
                   t.TxDate,
                   t.CategoryId,
                   c.Name AS CategoryName,
                   c.Type AS CategoryType,
                   t.Amount,
                   u.FullName AS UserName,
                   t.Comment
              FROM TRANSACTIONS t
              JOIN CATEGORIES c ON c.Id = t.CategoryId
              JOIN USERS u      ON u.Id = t.UserId" + FilterSql + @"
             ORDER BY t.TxDate DESC, t.Id DESC
             LIMIT @Limit OFFSET @Offset;";

        return connection.Query<TransactionView>(sql, CreateFilterParameters(filter)).AsList();
    }

    public Transaction? GetById(int id)
    {
        using var connection = _factory.Create();
        return connection.QuerySingleOrDefault<Transaction>(
            @"SELECT Id, UserId, CategoryId, TxDate, Amount, Comment, CreatedAt
              FROM TRANSACTIONS
              WHERE Id = @Id;",
            new { Id = id });
    }

    public int Add(Transaction transaction)
    {
        using var connection = _factory.Create();
        return connection.ExecuteScalar<int>(
            @"INSERT INTO TRANSACTIONS(UserId, CategoryId, TxDate, Amount, Comment, CreatedAt)
              VALUES(@UserId, @CategoryId, @TxDate, @Amount, @Comment, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                transaction.UserId,
                transaction.CategoryId,
                TxDate = transaction.TxDate.ToString("yyyy-MM-dd"),
                transaction.Amount,
                transaction.Comment,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }

    public bool Update(Transaction transaction)
    {
        using var connection = _factory.Create();
        var affected = connection.Execute(
            @"UPDATE TRANSACTIONS
              SET UserId = @UserId,
                  CategoryId = @CategoryId,
                  TxDate = @TxDate,
                  Amount = @Amount,
                  Comment = @Comment
              WHERE Id = @Id;",
            new
            {
                transaction.Id,
                transaction.UserId,
                transaction.CategoryId,
                TxDate = transaction.TxDate.ToString("yyyy-MM-dd"),
                transaction.Amount,
                transaction.Comment
            });

        return affected > 0;
    }

    public bool Delete(int id)
    {
        using var connection = _factory.Create();
        var affected = connection.Execute(
            "DELETE FROM TRANSACTIONS WHERE Id = @Id;",
            new { Id = id });

        return affected > 0;
    }

    public int CountByCategory(int categoryId)
    {
        using var connection = _factory.Create();
        return connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM TRANSACTIONS WHERE CategoryId = @CategoryId;",
            new { CategoryId = categoryId });
    }

    public IReadOnlyList<decimal> GetAmounts(TransactionFilter filter)
    {
        using var connection = _factory.Create();
        var sql = @"
            SELECT t.Amount
              FROM TRANSACTIONS t
              JOIN CATEGORIES c ON c.Id = t.CategoryId" + FilterSql + @"
             ORDER BY t.TxDate, t.Id;";

        return connection.Query<decimal>(sql, CreateFilterParameters(filter)).AsList();
    }

    public IReadOnlyList<MonthlyTotal> GetMonthlyTotals(DateTime from, DateTime to)
    {
        using var connection = _factory.Create();
        return connection.Query<MonthlyTotal>(
            @"SELECT strftime('%Y-%m', t.TxDate) AS Month,
                     c.Type AS Type,
                     SUM(t.Amount) AS Amount
                FROM TRANSACTIONS t
                JOIN CATEGORIES c ON c.Id = t.CategoryId
               WHERE date(t.TxDate) BETWEEN date(@From) AND date(@To)
               GROUP BY strftime('%Y-%m', t.TxDate), c.Type
               ORDER BY Month, c.Type;",
            new
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd")
            }).AsList();
    }

    public IReadOnlyList<CategoryTotal> GetCategoryTotals(DateTime from, DateTime to, CategoryType? type = null)
    {
        using var connection = _factory.Create();
        return connection.Query<CategoryTotal>(
            @"SELECT c.Name AS CategoryName,
                     SUM(t.Amount) AS Amount
                FROM TRANSACTIONS t
                JOIN CATEGORIES c ON c.Id = t.CategoryId
               WHERE date(t.TxDate) BETWEEN date(@From) AND date(@To)
                 AND (@Type IS NULL OR c.Type = @Type)
               GROUP BY c.Id, c.Name
               ORDER BY Amount DESC;",
            new
            {
                From = from.ToString("yyyy-MM-dd"),
                To = to.ToString("yyyy-MM-dd"),
                Type = type?.ToString()
            }).AsList();
    }

    private static object CreateFilterParameters(TransactionFilter filter) => new
    {
        From = filter.From?.ToString("yyyy-MM-dd"),
        To = filter.To?.ToString("yyyy-MM-dd"),
        CategoryId = filter.CategoryId,
        Type = filter.Type?.ToString(),
        Search = string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
        Limit = filter.Limit <= 0 ? 500 : filter.Limit,
        Offset = Math.Max(0, filter.Offset)
    };
}
