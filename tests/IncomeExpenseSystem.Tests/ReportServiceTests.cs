using FluentAssertions;
using IncomeExpenseSystem.App.Repositories;
using IncomeExpenseSystem.App.Services;
using Xunit;

namespace IncomeExpenseSystem.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public void BuildReport_ComputesIncomeExpenseAndBalance()
    {
        using var database = new TestDatabase();
        var repository = new TransactionRepository(database.Factory);
        var service = new ReportService(repository);

        var report = service.BuildReport(
            DateTime.Today.AddYears(-1),
            DateTime.Today.AddDays(1),
            categoryId: null,
            type: null,
            generatedBy: "Тест");

        report.Rows.Should().HaveCount(10);
        report.TotalIncome.Should().BeGreaterThan(0m);
        report.TotalExpense.Should().BeGreaterThan(0m);
        report.Balance.Should().Be(report.TotalIncome - report.TotalExpense);
        report.GeneratedBy.Should().Be("Тест");
    }

    [Fact]
    public void BuildReport_AppliesCategoryFilter()
    {
        using var database = new TestDatabase();
        var categories = new CategoryRepository(database.Factory);
        var repository = new TransactionRepository(database.Factory);
        var service = new ReportService(repository);
        var rent = categories.GetByName("Аренда")!;

        var report = service.BuildReport(
            DateTime.Today.AddYears(-1),
            DateTime.Today.AddDays(1),
            rent.Id,
            null,
            "Тест");

        report.Rows.Should().NotBeEmpty();
        report.Rows.Should().OnlyContain(row => row.CategoryId == rent.Id);
    }

    [Fact]
    public void ExportToExcel_CreatesNonEmptyFile()
    {
        using var database = new TestDatabase();
        var repository = new TransactionRepository(database.Factory);
        var service = new ReportService(repository);
        var report = service.BuildReport(
            DateTime.Today.AddYears(-1),
            DateTime.Today.AddDays(1),
            null,
            null,
            "Тест");

        var path = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.xlsx");

        try
        {
            service.ExportToExcel(report, path);

            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportToPdf_CreatesNonEmptyFile()
    {
        var arial = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "arial.ttf");

        if (!File.Exists(arial))
            return; // На среде без Arial тест не имеет смысла.

        using var database = new TestDatabase();
        var repository = new TransactionRepository(database.Factory);
        var service = new ReportService(repository);
        var report = service.BuildReport(
            DateTime.Today.AddYears(-1),
            DateTime.Today.AddDays(1),
            null,
            null,
            "Тест");

        var path = Path.Combine(Path.GetTempPath(), $"report_{Guid.NewGuid():N}.pdf");

        try
        {
            service.ExportToPdf(report, path);

            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
