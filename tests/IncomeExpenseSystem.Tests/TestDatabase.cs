using IncomeExpenseSystem.App.Database;
using Microsoft.Data.Sqlite;

namespace IncomeExpenseSystem.Tests;

/// <summary>Временная база SQLite для интеграционных тестов.</summary>
public sealed class TestDatabase : IDisposable
{
    public string DatabasePath { get; }

    public IDbConnectionFactory Factory { get; }

    public TestDatabase()
    {
        DatabasePath = Path.Combine(
            Path.GetTempPath(),
            $"incomeexpense_tests_{Guid.NewGuid():N}.db");

        Factory = new SqliteConnectionFactory($"Data Source={DatabasePath};Cache=Shared");
        DatabaseHelper.Initialize(Factory);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var file in new[] { DatabasePath, DatabasePath + "-wal", DatabasePath + "-shm" })
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Временный файл может быть занят антивирусом — не мешаем тестам.
            }
        }
    }
}
