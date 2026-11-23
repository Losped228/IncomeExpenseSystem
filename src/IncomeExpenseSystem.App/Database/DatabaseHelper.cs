using System.Data;
using Dapper;
using IncomeExpenseSystem.App.Models;
using Microsoft.Data.Sqlite;

namespace IncomeExpenseSystem.App.Database;

/// <summary>
/// Фабрика соединений. Позволяет подменять базу данных в тестах.
/// </summary>
public interface IDbConnectionFactory
{
    SqliteConnection Create();
}

/// <summary>
/// Создаёт открытое соединение SQLite и включает необходимые PRAGMA.
/// </summary>
public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Строка подключения не задана.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public SqliteConnection Create()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // SQLite NOCASE сравнивает только ASCII. Для русского текста
        // регистронезависимость обеспечивает пользовательская коллация.
        connection.CreateCollation("NOCASE_UNICODE",
            (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        command.ExecuteNonQuery();

        return connection;
    }
}

/// <summary>
/// Инициализация схемы, регистрация обработчиков типов и тестовые данные.
/// </summary>
public static class DatabaseHelper
{
    private static bool _handlersRegistered;

    /// <summary>
    /// Dapper не умеет автоматически преобразовывать TEXT в enum,
    /// поэтому регистрируем собственные обработчики.
    /// </summary>
    public static void RegisterTypeHandlers()
    {
        if (_handlersRegistered)
            return;

        SqlMapper.AddTypeHandler(new UserRoleHandler());
        SqlMapper.AddTypeHandler(new CategoryTypeHandler());
        _handlersRegistered = true;
    }

    /// <summary>
    /// Создаёт таблицы (если их нет) и заполняет базу тестовыми данными.
    /// </summary>
    public static void Initialize(IDbConnectionFactory factory)
    {
        RegisterTypeHandlers();

        using var connection = factory.Create();
        connection.Execute(Schema);
        Seed(connection);
    }

    private const string Schema = @"
CREATE TABLE IF NOT EXISTS USERS (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Login        TEXT NOT NULL COLLATE NOCASE_UNICODE UNIQUE CHECK (length(Login) BETWEEN 3 AND 50),
    PasswordHash TEXT NOT NULL,
    FullName     TEXT NOT NULL,
    Role         TEXT NOT NULL CHECK (Role IN ('Admin','Accountant','Analyst')),
    IsActive     INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt    TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS CATEGORIES (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT NOT NULL COLLATE NOCASE_UNICODE UNIQUE CHECK (length(Name) BETWEEN 2 AND 100),
    Type        TEXT NOT NULL CHECK (Type IN ('Income','Expense')),
    Description TEXT,
    IsActive    INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt   TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS TRANSACTIONS (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId     INTEGER NOT NULL,
    CategoryId INTEGER NOT NULL,
    TxDate     TEXT NOT NULL CHECK (date(TxDate) IS NOT NULL),
    Amount     REAL NOT NULL CHECK (Amount > 0),
    Comment    TEXT,
    CreatedAt  TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (UserId)     REFERENCES USERS(Id)      ON DELETE RESTRICT,
    FOREIGN KEY (CategoryId) REFERENCES CATEGORIES(Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS idx_tx_date ON TRANSACTIONS(TxDate);
CREATE INDEX IF NOT EXISTS idx_tx_category ON TRANSACTIONS(CategoryId);
CREATE INDEX IF NOT EXISTS idx_tx_user ON TRANSACTIONS(UserId);
";

    private static void Seed(SqliteConnection connection)
    {
        var userCount = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM USERS;");
        if (userCount > 0)
            return;

        using var transaction = connection.BeginTransaction();

        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var adminId = connection.ExecuteScalar<int>(
            @"INSERT INTO USERS(Login, PasswordHash, FullName, Role, IsActive, CreatedAt)
              VALUES(@Login, @PasswordHash, @FullName, @Role, 1, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12),
                FullName = "Администратор системы",
                Role = UserRole.Admin.ToString(),
                CreatedAt = now
            }, transaction);

        var accountantId = connection.ExecuteScalar<int>(
            @"INSERT INTO USERS(Login, PasswordHash, FullName, Role, IsActive, CreatedAt)
              VALUES(@Login, @PasswordHash, @FullName, @Role, 1, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                Login = "accountant",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("acc123", workFactor: 12),
                FullName = "Иванова Мария Петровна",
                Role = UserRole.Accountant.ToString(),
                CreatedAt = now
            }, transaction);

        _ = connection.ExecuteScalar<int>(
            @"INSERT INTO USERS(Login, PasswordHash, FullName, Role, IsActive, CreatedAt)
              VALUES(@Login, @PasswordHash, @FullName, @Role, 1, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                Login = "analyst",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("an123", workFactor: 12),
                FullName = "Петров Алексей Сергеевич",
                Role = UserRole.Analyst.ToString(),
                CreatedAt = now
            }, transaction);

        var incomeId = InsertCategory(connection, transaction,
            "Выручка от продаж", CategoryType.Income, "Основной доход от реализации товаров и услуг", now);

        var otherIncomeId = InsertCategory(connection, transaction,
            "Прочие доходы", CategoryType.Income, "Проценты, штрафы, прочие поступления", now);

        var rentId = InsertCategory(connection, transaction,
            "Аренда", CategoryType.Expense, "Аренда офиса и оборудования", now);

        var salaryId = InsertCategory(connection, transaction,
            "Зарплата", CategoryType.Expense, "Оплата труда сотрудников", now);

        var materialsId = InsertCategory(connection, transaction,
            "Материалы", CategoryType.Expense, "Канцелярия, расходные материалы", now);

        var today = DateTime.Today;
        var samples = new (int CategoryId, decimal Amount, string Comment, DateTime Date)[]
        {
            (incomeId, 120_000m, "Выручка за текущий месяц", today.AddDays(-10)),
            (incomeId, 150_000m, "Выручка за прошлый месяц", today.AddDays(-40)),
            (otherIncomeId, 45_000m, "Проценты по депозиту", today.AddDays(-5)),
            (rentId, 30_000m, "Аренда офиса", today.AddDays(-9)),
            (rentId, 30_000m, "Аренда офиса за прошлый месяц", today.AddDays(-39)),
            (salaryId, 85_000m, "Зарплата за текущий месяц", today.AddDays(-8)),
            (salaryId, 82_000m, "Зарплата за прошлый месяц", today.AddDays(-38)),
            (materialsId, 12_000m, "Канцелярия и бумага", today.AddDays(-7)),
            (materialsId, 9_000m, "Расходные материалы", today.AddDays(-35)),
            (materialsId, 7_000m, "Хозяйственные товары", today.AddDays(-65))
        };

        foreach (var sample in samples)
        {
            connection.Execute(
                @"INSERT INTO TRANSACTIONS(UserId, CategoryId, TxDate, Amount, Comment, CreatedAt)
                  VALUES(@UserId, @CategoryId, @TxDate, @Amount, @Comment, @CreatedAt);",
                new
                {
                    UserId = accountantId,
                    CategoryId = sample.CategoryId,
                    TxDate = sample.Date.ToString("yyyy-MM-dd"),
                    Amount = sample.Amount,
                    Comment = sample.Comment,
                    CreatedAt = now
                }, transaction);
        }

        transaction.Commit();
    }

    private static int InsertCategory(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string name,
        CategoryType type,
        string description,
        string createdAt)
    {
        return connection.ExecuteScalar<int>(
            @"INSERT INTO CATEGORIES(Name, Type, Description, IsActive, CreatedAt)
              VALUES(@Name, @Type, @Description, 1, @CreatedAt);
              SELECT last_insert_rowid();",
            new { Name = name, Type = type.ToString(), Description = description, CreatedAt = createdAt },
            transaction);
    }

    /// <summary>Преобразование TEXT → UserRole.</summary>
    private sealed class UserRoleHandler : SqlMapper.TypeHandler<UserRole>
    {
        public override void SetValue(IDbDataParameter parameter, UserRole value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }

        public override UserRole Parse(object value) => value switch
        {
            string text => Enum.Parse<UserRole>(text, ignoreCase: true),
            _ => (UserRole)Convert.ToInt32(value)
        };
    }

    /// <summary>Преобразование TEXT → CategoryType.</summary>
    private sealed class CategoryTypeHandler : SqlMapper.TypeHandler<CategoryType>
    {
        public override void SetValue(IDbDataParameter parameter, CategoryType value)
        {
            parameter.Value = value.ToString();
            parameter.DbType = DbType.String;
        }

        public override CategoryType Parse(object value) => value switch
        {
            string text => Enum.Parse<CategoryType>(text, ignoreCase: true),
            _ => (CategoryType)Convert.ToInt32(value)
        };
    }
}
