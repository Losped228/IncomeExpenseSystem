using Dapper;
using FluentAssertions;
using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using Microsoft.Data.Sqlite;
using Xunit;

namespace IncomeExpenseSystem.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public void Initialize_SeedsThreeUsersFiveCategoriesAndTenTransactions()
    {
        using var database = new TestDatabase();
        using var connection = database.Factory.Create();

        connection.ExecuteScalar<long>("SELECT COUNT(*) FROM USERS;").Should().Be(3);
        connection.ExecuteScalar<long>("SELECT COUNT(*) FROM CATEGORIES;").Should().Be(5);
        connection.ExecuteScalar<long>("SELECT COUNT(*) FROM TRANSACTIONS;").Should().Be(10);
    }

    [Fact]
    public void UserRepository_FindsUserByLoginIgnoringCase()
    {
        using var database = new TestDatabase();
        var repository = new UserRepository(database.Factory);

        var user = repository.GetByLogin("ADMIN");

        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Admin);
        BCrypt.Net.BCrypt.Verify("admin123", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public void CategoryRepository_FindsRussianNameIgnoringCase()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Factory);

        var category = repository.GetByName("ВЫРУЧКА ОТ ПРОДАЖ");

        category.Should().NotBeNull();
        category!.Type.Should().Be(CategoryType.Income);
    }

    [Fact]
    public void TransactionRepository_FiltersByTypeAndPeriod()
    {
        using var database = new TestDatabase();
        var repository = new TransactionRepository(database.Factory);

        var rows = repository.GetFiltered(new TransactionFilter
        {
            From = DateTime.Today.AddYears(-1),
            To = DateTime.Today.AddDays(1),
            Type = CategoryType.Income,
            Limit = 1_000
        });

        rows.Should().HaveCount(3);
        rows.Should().OnlyContain(row => row.CategoryType == CategoryType.Income);
    }

    [Fact]
    public void TransactionRepository_AddsAndReadsTransaction()
    {
        using var database = new TestDatabase();
        var users = new UserRepository(database.Factory);
        var categories = new CategoryRepository(database.Factory);
        var repository = new TransactionRepository(database.Factory);

        var user = users.GetByLogin("accountant")!;
        var category = categories.GetByName("Материалы")!;

        var id = repository.Add(new Transaction
        {
            UserId = user.Id,
            CategoryId = category.Id,
            TxDate = DateTime.Today,
            Amount = 1234.56m,
            Comment = "Тестовая операция"
        });

        var loaded = repository.GetById(id);

        loaded.Should().NotBeNull();
        loaded!.CategoryId.Should().Be(category.Id);
        loaded.UserId.Should().Be(user.Id);
        Math.Round(loaded.Amount, 2).Should().Be(1234.56m);
        loaded.Comment.Should().Be("Тестовая операция");
    }

    [Fact]
    public void CategoryRepository_DeleteUsedCategory_ThrowsForeignKeyViolation()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Factory);
        var category = repository.GetByName("Аренда")!;

        var action = () => repository.Delete(category.Id);

        action.Should().Throw<SqliteException>();
    }

    [Fact]
    public void TransactionRepository_ReturnsAmountsAndCategoryTotals()
    {
        using var database = new TestDatabase();
        var repository = new TransactionRepository(database.Factory);
        var filter = new TransactionFilter
        {
            From = DateTime.Today.AddYears(-1),
            To = DateTime.Today.AddDays(1),
            Limit = 1_000
        };

        var amounts = repository.GetAmounts(filter);
        var expenseTotals = repository.GetCategoryTotals(
            DateTime.Today.AddYears(-1),
            DateTime.Today.AddDays(1),
            CategoryType.Expense);

        amounts.Should().HaveCount(10);
        amounts.Should().OnlyContain(amount => amount > 0m);
        expenseTotals.Should().NotBeEmpty();
        expenseTotals.Should().OnlyContain(item => item.Amount > 0m);
    }

    [Fact]
    public void TransactionRepository_ReturnsMonthlyTotals()
    {
        using var database = new TestDatabase();
        var repository = new TransactionRepository(database.Factory);

        var totals = repository.GetMonthlyTotals(
            DateTime.Today.AddYears(-1),
            DateTime.Today.AddDays(1));

        totals.Should().NotBeEmpty();
        totals.Should().Contain(item => item.Type == CategoryType.Income);
        totals.Should().Contain(item => item.Type == CategoryType.Expense);
    }
}
