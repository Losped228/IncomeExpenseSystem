using System.Globalization;
using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Serilog;
using ITextBaseColor = iTextSharp.text.BaseColor;
using ITextBaseFont = iTextSharp.text.pdf.BaseFont;
using ITextDocument = iTextSharp.text.Document;
using ITextElement = iTextSharp.text.Element;
using ITextFont = iTextSharp.text.Font;
using ITextPageSize = iTextSharp.text.PageSize;
using ITextParagraph = iTextSharp.text.Paragraph;
using ITextPdfCell = iTextSharp.text.pdf.PdfPCell;
using ITextPdfTable = iTextSharp.text.pdf.PdfPTable;
using ITextPdfWriter = iTextSharp.text.pdf.PdfWriter;
using ITextPhrase = iTextSharp.text.Phrase;

namespace IncomeExpenseSystem.App.Services;

/// <summary>
/// Формирование отчёта по операциям и экспорт в Excel/PDF.
/// </summary>
public sealed class ReportService
{
    private readonly ITransactionRepository _transactions;

    public ReportService(ITransactionRepository transactions)
    {
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    }

    /// <summary>Собирает данные отчёта за период с учётом фильтров.</summary>
    public ReportData BuildReport(
        DateTime from,
        DateTime to,
        int? categoryId,
        CategoryType? type,
        string generatedBy)
    {
        if (to.Date < from.Date)
            throw new ArgumentException("Дата окончания не может быть раньше даты начала.");

        var rows = _transactions.GetFiltered(new TransactionFilter
        {
            From = from.Date,
            To = to.Date,
            CategoryId = categoryId,
            Type = type,
            Limit = 100_000
        });

        return new ReportData
        {
            From = from.Date,
            To = to.Date,
            GeneratedAt = DateTime.Now,
            GeneratedBy = string.IsNullOrWhiteSpace(generatedBy) ? "Система" : generatedBy,
            Rows = rows,
            TotalIncome = rows.Where(r => r.CategoryType == CategoryType.Income).Sum(r => r.Amount),
            TotalExpense = rows.Where(r => r.CategoryType == CategoryType.Expense).Sum(r => r.Amount)
        };
    }

    /// <summary>Экспорт отчёта в XLSX средствами EPPlus.</summary>
    public void ExportToExcel(ReportData report, string filePath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ValidateFilePath(filePath);

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Отчёт");

            sheet.Cells["A1:G1"].Merge = true;
            sheet.Cells["A1"].Value = "Отчёт по доходам и расходам";
            sheet.Cells["A1"].Style.Font.Size = 16;
            sheet.Cells["A1"].Style.Font.Bold = true;
            sheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            sheet.Cells["A2"].Value = $"Период: {report.From:dd.MM.yyyy} — {report.To:dd.MM.yyyy}";
            sheet.Cells["A3"].Value = $"Сформировал: {report.GeneratedBy}; {report.GeneratedAt:dd.MM.yyyy HH:mm}";

            var headers = new[] { "№", "Дата", "Категория", "Тип", "Сумма, руб.", "Автор", "Комментарий" };
            for (var column = 0; column < headers.Length; column++)
                sheet.Cells[4, column + 1].Value = headers[column];

            using (var header = sheet.Cells[4, 1, 4, headers.Length])
            {
                header.Style.Font.Bold = true;
                header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(219, 229, 241));
                header.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            var row = 5;
            var number = 1;
            foreach (var item in report.Rows)
            {
                sheet.Cells[row, 1].Value = number++;
                sheet.Cells[row, 2].Value = item.TxDate;
                sheet.Cells[row, 2].Style.Numberformat.Format = "dd.MM.yyyy";
                sheet.Cells[row, 3].Value = item.CategoryName;
                sheet.Cells[row, 4].Value = item.TypeDisplayName;
                sheet.Cells[row, 5].Value = item.Amount;
                sheet.Cells[row, 5].Style.Numberformat.Format = "#,##0.00";
                sheet.Cells[row, 6].Value = item.UserName;
                sheet.Cells[row, 7].Value = item.Comment;
                row++;
            }

            var incomeRow = row;
            sheet.Cells[incomeRow, 1, incomeRow, 4].Merge = true;
            sheet.Cells[incomeRow, 1].Value = "Итого доходы";
            sheet.Cells[incomeRow, 5].Value = report.TotalIncome;
            sheet.Cells[incomeRow, 5].Style.Numberformat.Format = "#,##0.00";

            var expenseRow = row + 1;
            sheet.Cells[expenseRow, 1, expenseRow, 4].Merge = true;
            sheet.Cells[expenseRow, 1].Value = "Итого расходы";
            sheet.Cells[expenseRow, 5].Value = report.TotalExpense;
            sheet.Cells[expenseRow, 5].Style.Numberformat.Format = "#,##0.00";

            var balanceRow = row + 2;
            sheet.Cells[balanceRow, 1, balanceRow, 4].Merge = true;
            sheet.Cells[balanceRow, 1].Value = "Баланс";
            sheet.Cells[balanceRow, 5].Value = report.Balance;
            sheet.Cells[balanceRow, 5].Style.Numberformat.Format = "#,##0.00";

            using (var totals = sheet.Cells[incomeRow, 1, balanceRow, headers.Length])
            {
                totals.Style.Font.Bold = true;
                totals.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            }

            sheet.Cells[4, 1, Math.Max(4, row - 1), headers.Length].AutoFilter = true;
            sheet.View.FreezePanes(5, 1);

            sheet.Column(1).Width = 6;
            sheet.Column(2).Width = 12;
            sheet.Column(3).Width = 30;
            sheet.Column(4).Width = 11;
            sheet.Column(5).Width = 15;
            sheet.Column(6).Width = 26;
            sheet.Column(7).Width = 45;

            for (var r = 5; r <= Math.Max(5, row - 1); r++)
                sheet.Cells[r, 7].Style.WrapText = true;

            package.SaveAs(new FileInfo(filePath));
            Log.Information("Отчёт Excel сохранён: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка экспорта отчёта в Excel: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>Экспорт отчёта в PDF средствами iTextSharp.</summary>
    public void ExportToPdf(ReportData report, string filePath)
    {
        ArgumentNullException.ThrowIfNull(report);
        ValidateFilePath(filePath);

        ITextDocument? document = null;
        FileStream? output = null;

        try
        {
            var fontPath = FindFontPath();
            var baseFont = ITextBaseFont.CreateFont(fontPath, ITextBaseFont.IDENTITY_H, ITextBaseFont.EMBEDDED);

            var titleFont = new ITextFont(baseFont, 16, ITextFont.BOLD, new ITextBaseColor(31, 56, 100));
            var normalFont = new ITextFont(baseFont, 9, ITextFont.NORMAL, new ITextBaseColor(40, 40, 40));
            var boldFont = new ITextFont(baseFont, 10, ITextFont.BOLD, new ITextBaseColor(40, 40, 40));

            output = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            document = new ITextDocument(ITextPageSize.A4.Rotate(), 20f, 20f, 30f, 30f);
            ITextPdfWriter.GetInstance(document, output);
            document.Open();

            document.Add(new ITextParagraph("Отчёт по доходам и расходам", titleFont)
            {
                Alignment = ITextElement.ALIGN_CENTER,
                SpacingAfter = 10f
            });

            document.Add(new ITextParagraph(
                $"Период: {report.From:dd.MM.yyyy} — {report.To:dd.MM.yyyy}",
                normalFont) { SpacingAfter = 3f });

            document.Add(new ITextParagraph(
                $"Сформировал: {report.GeneratedBy}; дата формирования: {report.GeneratedAt:dd.MM.yyyy HH:mm}",
                normalFont) { SpacingAfter = 10f });

            var table = new ITextPdfTable(7) { WidthPercentage = 100f, HeaderRows = 1 };
            table.SetWidths(new float[] { 4f, 9f, 20f, 8f, 12f, 18f, 29f });

            AddCell(table, "№", boldFont, ITextElement.ALIGN_CENTER, true);
            AddCell(table, "Дата", boldFont, ITextElement.ALIGN_CENTER, true);
            AddCell(table, "Категория", boldFont, ITextElement.ALIGN_CENTER, true);
            AddCell(table, "Тип", boldFont, ITextElement.ALIGN_CENTER, true);
            AddCell(table, "Сумма, руб.", boldFont, ITextElement.ALIGN_CENTER, true);
            AddCell(table, "Автор", boldFont, ITextElement.ALIGN_CENTER, true);
            AddCell(table, "Комментарий", boldFont, ITextElement.ALIGN_CENTER, true);

            var number = 1;
            foreach (var item in report.Rows)
            {
                AddCell(table, number++.ToString(CultureInfo.InvariantCulture), normalFont, ITextElement.ALIGN_CENTER, false);
                AddCell(table, item.TxDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture), normalFont, ITextElement.ALIGN_CENTER, false);
                AddCell(table, item.CategoryName, normalFont, ITextElement.ALIGN_LEFT, false);
                AddCell(table, item.TypeDisplayName, normalFont, ITextElement.ALIGN_CENTER, false);
                AddCell(table, item.Amount.ToString("N2", CultureInfo.CurrentCulture), normalFont, ITextElement.ALIGN_RIGHT, false);
                AddCell(table, item.UserName, normalFont, ITextElement.ALIGN_LEFT, false);
                AddCell(table, item.Comment ?? string.Empty, normalFont, ITextElement.ALIGN_LEFT, false);
            }

            if (report.Rows.Count == 0)
            {
                AddCell(table, "Нет операций за выбранный период", normalFont, ITextElement.ALIGN_CENTER, false);
                for (var i = 0; i < 6; i++)
                    AddCell(table, string.Empty, normalFont, ITextElement.ALIGN_LEFT, false);
            }

            document.Add(table);
            document.Add(new ITextParagraph(" ", normalFont));
            document.Add(new ITextParagraph($"Итого доходы: {report.TotalIncome:N2} руб.", boldFont)
            {
                Alignment = ITextElement.ALIGN_RIGHT,
                SpacingAfter = 2f
            });
            document.Add(new ITextParagraph($"Итого расходы: {report.TotalExpense:N2} руб.", boldFont)
            {
                Alignment = ITextElement.ALIGN_RIGHT,
                SpacingAfter = 2f
            });
            document.Add(new ITextParagraph($"Баланс: {report.Balance:N2} руб.", boldFont)
            {
                Alignment = ITextElement.ALIGN_RIGHT
            });

            Log.Information("Отчёт PDF сохранён: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка экспорта отчёта в PDF: {FilePath}", filePath);
            throw;
        }
        finally
        {
            if (document is not null && document.IsOpen())
                document.Close();

            output?.Dispose();
        }
    }

    private static void AddCell(
        ITextPdfTable table,
        string text,
        ITextFont font,
        int alignment,
        bool isHeader)
    {
        var cell = new ITextPdfCell(new ITextPhrase(text, font))
        {
            Padding = 4f,
            HorizontalAlignment = alignment,
            VerticalAlignment = ITextElement.ALIGN_MIDDLE,
            BackgroundColor = isHeader
                ? new ITextBaseColor(219, 229, 241)
                : new ITextBaseColor(255, 255, 255)
        };

        table.AddCell(cell);
    }

    private static string FindFontPath()
    {
        var fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "fonts", "DejaVuSans.ttf"),
            Path.Combine(fontsDirectory, "arial.ttf"),
            Path.Combine(fontsDirectory, "times.ttf")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException(
            "Не найден шрифт с поддержкой кириллицы. Добавьте fonts/DejaVuSans.ttf в папку приложения.");
    }

    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не задан.", nameof(filePath));

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}
