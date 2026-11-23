using System.Globalization;
using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using ScottPlot;
using Serilog;

namespace IncomeExpenseSystem.App.Forms;

/// <summary>Дашборд с графиками доходов, расходов и структуры затрат.</summary>
public sealed class DashboardForm : Form
{
    private readonly User _user;
    private readonly ITransactionRepository _transactions;

    private readonly NumericUpDown _year = new();
    private readonly Label _lblIncome = new();
    private readonly Label _lblExpense = new();
    private readonly Label _lblBalance = new();

    private readonly FormsPlot _plotMonthly = new() { Dock = DockStyle.Fill };
    private readonly FormsPlot _plotCategories = new() { Dock = DockStyle.Fill };

    public DashboardForm(User user, ITransactionRepository transactions)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));

        InitializeUi();
        Load += (_, _) => RefreshDashboard();
    }

    private void InitializeUi()
    {
        Text = $"Дашборд — {_user.FullName}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 620);
        ClientSize = new Size(1180, 760);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildCharts(), 0, 1);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(3)
        };

        panel.Controls.Add(new Label
        {
            Text = "Год:",
            AutoSize = true,
            Margin = new Padding(3, 10, 3, 0)
        });

        _year.Minimum = 2000;
        _year.Maximum = 2100;
        _year.Value = DateTime.Today.Year;
        _year.Width = 85;
        _year.Margin = new Padding(3, 5, 10, 5);
        panel.Controls.Add(_year);

        var btnRefresh = new Button
        {
            Text = "Обновить",
            Size = new Size(105, 30),
            Margin = new Padding(0, 4, 18, 4)
        };
        btnRefresh.Click += (_, _) => RefreshDashboard();
        panel.Controls.Add(btnRefresh);

        ConfigureSummaryLabel(_lblIncome, Color.FromArgb(46, 139, 87));
        ConfigureSummaryLabel(_lblExpense, Color.FromArgb(178, 34, 34));
        ConfigureSummaryLabel(_lblBalance, Color.FromArgb(31, 56, 100));

        panel.Controls.Add(_lblIncome);
        panel.Controls.Add(_lblExpense);
        panel.Controls.Add(_lblBalance);

        return panel;
    }

    private static void ConfigureSummaryLabel(Label label, Color color)
    {
        label.Text = "—";
        label.AutoSize = true;
        label.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        label.ForeColor = color;
        label.Margin = new Padding(0, 8, 20, 0);
    }

    private Control BuildCharts()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 8, 0, 0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _plotMonthly.Margin = new Padding(0, 0, 8, 0);
        _plotCategories.Margin = new Padding(8, 0, 0, 0);

        layout.Controls.Add(_plotMonthly, 0, 0);
        layout.Controls.Add(_plotCategories, 1, 0);

        return layout;
    }

    private void RefreshDashboard()
    {
        try
        {
            var year = (int)_year.Value;
            var from = new DateTime(year, 1, 1);
            var to = new DateTime(year, 12, 31);

            var monthly = _transactions.GetMonthlyTotals(from, to);
            var expenseByCategory = _transactions.GetCategoryTotals(from, to, CategoryType.Expense);

            var incomeValues = new double[12];
            var expenseValues = new double[12];

            foreach (var item in monthly)
            {
                var parts = item.Month.Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[1], out var month))
                    continue;

                if (month is < 1 or > 12)
                    continue;

                if (item.Type == CategoryType.Income)
                    incomeValues[month - 1] += (double)item.Amount;
                else
                    expenseValues[month - 1] += (double)item.Amount;
            }

            var totalIncome = incomeValues.Sum();
            var totalExpense = expenseValues.Sum();

            _lblIncome.Text = $"Доходы: {totalIncome:N2} руб.";
            _lblExpense.Text = $"Расходы: {totalExpense:N2} руб.";
            _lblBalance.Text = $"Баланс: {totalIncome - totalExpense:N2} руб.";

            DrawMonthlyChart(year, incomeValues, expenseValues);
            DrawExpensePieChart(expenseByCategory);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка построения дашборда");
            MessageBox.Show(this, "Не удалось построить графики. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DrawMonthlyChart(int year, double[] incomeValues, double[] expenseValues)
    {
        _plotMonthly.Plot.Clear();

        var positions = Enumerable.Range(0, 12).Select(i => (double)i).ToArray();
        var incomePositions = positions.Select(p => p - 0.2).ToArray();
        var expensePositions = positions.Select(p => p + 0.2).ToArray();

        var incomeBars = _plotMonthly.Plot.AddBar(incomeValues, incomePositions);
        incomeBars.BarWidth = 0.38;
        incomeBars.FillColor = Color.FromArgb(46, 139, 87);
        incomeBars.Label = "Доходы";

        var expenseBars = _plotMonthly.Plot.AddBar(expenseValues, expensePositions);
        expenseBars.BarWidth = 0.38;
        expenseBars.FillColor = Color.FromArgb(205, 92, 92);
        expenseBars.Label = "Расходы";

        var culture = new CultureInfo("ru-RU");
        var months = Enumerable.Range(1, 12)
            .Select(m => new DateTime(year, m, 1).ToString("MMM", culture).TrimEnd('.'))
            .ToArray();

        _plotMonthly.Plot.XTicks(positions, months);
        _plotMonthly.Plot.SetAxisLimitsY(0, Math.Max(1, Math.Max(incomeValues.Max(), expenseValues.Max()) * 1.2));
        _plotMonthly.Plot.Title("Доходы и расходы по месяцам");
        _plotMonthly.Plot.YLabel("Сумма, руб.");
        _plotMonthly.Plot.Legend();
        _plotMonthly.Refresh();
    }

    private void DrawExpensePieChart(IReadOnlyList<CategoryTotal> expenseByCategory)
    {
        _plotCategories.Plot.Clear();

        if (expenseByCategory.Count == 0)
        {
            _plotCategories.Plot.Title("Структура расходов: нет данных");
            _plotCategories.Refresh();
            return;
        }

        var values = expenseByCategory.Select(x => (double)x.Amount).ToArray();
        var labels = expenseByCategory.Select(x => x.CategoryName).ToArray();

#pragma warning disable CS0618 // PlotPie оставлен для совместимости со ScottPlot 4.x
        _plotCategories.Plot.PlotPie(values, labels);
#pragma warning restore CS0618
        _plotCategories.Plot.Title("Структура расходов");
        _plotCategories.Refresh();
    }
}
