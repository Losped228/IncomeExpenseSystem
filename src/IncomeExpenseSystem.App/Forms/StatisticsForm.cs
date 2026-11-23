using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using IncomeExpenseSystem.App.Services;
using Serilog;

namespace IncomeExpenseSystem.App.Forms;

/// <summary>Расчёт описательных статистик по выбранной выборке операций.</summary>
public sealed class StatisticsForm : Form
{
    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;
    private readonly StatisticsService _statistics;

    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly ComboBox _cmbCategory = new();
    private readonly ComboBox _cmbType = new();

    private readonly Label _lblCount = new();
    private readonly Label _lblSum = new();
    private readonly Label _lblMean = new();
    private readonly Label _lblMedian = new();
    private readonly Label _lblVariance = new();
    private readonly Label _lblDeviation = new();
    private readonly Label _lblMin = new();
    private readonly Label _lblMax = new();
    private readonly Label _lblCv = new();
    private readonly Label _lblInterpretation = new();

    public StatisticsForm(
        User user,
        ITransactionRepository transactions,
        ICategoryRepository categories,
        StatisticsService statistics)
    {
        ArgumentNullException.ThrowIfNull(user);
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _categories = categories ?? throw new ArgumentNullException(nameof(categories));
        _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));

        InitializeUi(user);
        Load += (_, _) =>
        {
            LoadCategories();
            Calculate();
        };
    }

    private void InitializeUi(User user)
    {
        Text = $"Статистика — {user.FullName}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 660);
        ClientSize = new Size(850, 720);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildFilters(), 0, 0);
        root.Controls.Add(BuildStatsTable(), 0, 1);
        root.Controls.Add(BuildInterpretation(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildFilters()
    {
        var group = new GroupBox
        {
            Text = "Период и фильтры",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(10)
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(3)
        };

        _from.Format = DateTimePickerFormat.Short;
        _from.Value = new DateTime(DateTime.Today.AddMonths(-11).Year,
            DateTime.Today.AddMonths(-11).Month, 1);
        _from.Width = 110;

        _to.Format = DateTimePickerFormat.Short;
        _to.Value = DateTime.Today;
        _to.Width = 110;

        _cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbCategory.Width = 230;

        _cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbType.Width = 130;
        _cmbType.Items.AddRange(new object[] { "Все типы", "Доходы", "Расходы" });
        _cmbType.SelectedIndex = 0;

        panel.Controls.Add(CreateLabel("С даты:"));
        panel.Controls.Add(_from);
        panel.Controls.Add(CreateLabel("По дату:"));
        panel.Controls.Add(_to);
        panel.Controls.Add(CreateLabel("Категория:"));
        panel.Controls.Add(_cmbCategory);
        panel.Controls.Add(CreateLabel("Тип:"));
        panel.Controls.Add(_cmbType);

        var btnCalculate = new Button
        {
            Text = "Рассчитать",
            Size = new Size(115, 30),
            Margin = new Padding(10, 4, 0, 4)
        };
        btnCalculate.Click += (_, _) => Calculate();
        panel.Controls.Add(btnCalculate);

        group.Controls.Add(panel);
        return group;
    }

    private Control BuildStatsTable()
    {
        var group = new GroupBox
        {
            Text = "Описательные статистики",
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(10)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var valueFont = new Font("Segoe UI", 10F, FontStyle.Bold);

        AddStatRow(table, 0, "Количество операций (n):", _lblCount, valueFont);
        AddStatRow(table, 1, "Сумма, руб.:", _lblSum, valueFont);
        AddStatRow(table, 2, "Среднее арифметическое, руб.:", _lblMean, valueFont);
        AddStatRow(table, 3, "Медиана, руб.:", _lblMedian, valueFont);
        AddStatRow(table, 4, "Дисперсия (выборочная):", _lblVariance, valueFont);
        AddStatRow(table, 5, "Среднеквадратическое отклонение:", _lblDeviation, valueFont);
        AddStatRow(table, 6, "Минимум, руб.:", _lblMin, valueFont);
        AddStatRow(table, 7, "Максимум, руб.:", _lblMax, valueFont);
        AddStatRow(table, 8, "Коэффициент вариации:", _lblCv, valueFont);

        group.Controls.Add(table);
        return group;
    }

    private Control BuildInterpretation()
    {
        var group = new GroupBox
        {
            Text = "Интерпретация",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12)
        };

        _lblInterpretation.Dock = DockStyle.Fill;
        _lblInterpretation.AutoSize = false;
        _lblInterpretation.MinimumSize = new Size(0, 52);
        _lblInterpretation.Text = "Нажмите «Рассчитать», чтобы получить статистики.";
        _lblInterpretation.ForeColor = Color.FromArgb(60, 60, 60);
        _lblInterpretation.Font = new Font("Segoe UI", 9F);

        group.Controls.Add(_lblInterpretation);
        return group;
    }

    private static void AddStatRow(
        TableLayoutPanel table,
        int row,
        string caption,
        Label valueLabel,
        Font valueFont)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };

        valueLabel.Text = "—";
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Font = valueFont;
        valueLabel.ForeColor = Color.FromArgb(31, 56, 100);

        table.Controls.Add(label, 0, row);
        table.Controls.Add(valueLabel, 1, row);
    }

    private void LoadCategories()
    {
        try
        {
            var categories = _categories.GetAll().ToList();

            _cmbCategory.Items.Clear();
            _cmbCategory.Items.Add(new CategoryOption(0, "Все категории"));

            foreach (var category in categories)
                _cmbCategory.Items.Add(new CategoryOption(category.Id, category.Name));

            _cmbCategory.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки категорий для статистики");
            MessageBox.Show(this, "Не удалось загрузить категории. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Calculate()
    {
        if (_to.Value.Date < _from.Value.Date)
        {
            MessageBox.Show(this, "Дата окончания не может быть раньше даты начала.",
                "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var filter = new TransactionFilter
            {
                From = _from.Value.Date,
                To = _to.Value.Date,
                CategoryId = _cmbCategory.SelectedItem is CategoryOption option && option.Id > 0
                    ? option.Id
                    : null,
                Type = _cmbType.SelectedIndex switch
                {
                    1 => CategoryType.Income,
                    2 => CategoryType.Expense,
                    _ => null
                },
                Limit = 100_000
            };

            var values = _transactions.GetAmounts(filter);

            if (values.Count == 0)
            {
                ShowEmpty();
                _lblInterpretation.Text = "За выбранный период операций нет — статистики не определены.";
                return;
            }

            var result = _statistics.Calculate(values);

            _lblCount.Text = result.Count.ToString("N0");
            _lblSum.Text = $"{result.Sum:N2} руб.";
            _lblMean.Text = $"{result.Mean:N2} руб.";
            _lblMedian.Text = $"{result.Median:N2} руб.";
            _lblVariance.Text = result.Variance.ToString("N2");
            _lblDeviation.Text = result.StandardDeviation.ToString("N2");
            _lblMin.Text = $"{result.Min:N2} руб.";
            _lblMax.Text = $"{result.Max:N2} руб.";
            _lblCv.Text = $"{result.CoefficientOfVariation:N2} %";
            _lblInterpretation.Text = Interpret(result.CoefficientOfVariation);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка расчёта статистик");
            ShowEmpty();
            _lblInterpretation.Text = "Не удалось рассчитать статистики. Подробности записаны в журнал.";
            MessageBox.Show(this, "Не удалось рассчитать статистики. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowEmpty()
    {
        _lblCount.Text = "0";
        _lblSum.Text = "—";
        _lblMean.Text = "—";
        _lblMedian.Text = "—";
        _lblVariance.Text = "—";
        _lblDeviation.Text = "—";
        _lblMin.Text = "—";
        _lblMax.Text = "—";
        _lblCv.Text = "—";
    }

    private static string Interpret(decimal coefficientOfVariation)
    {
        var value = (double)coefficientOfVariation;

        if (value <= 10)
            return "Коэффициент вариации ≤ 10 %: выборка однородная, среднее значение устойчиво.";

        if (value <= 25)
            return "Коэффициент вариации 10–25 %: разброс умеренный, среднее можно использовать для выводов.";

        return "Коэффициент вариации > 25 %: разброс высокий; среднее следует интерпретировать осторожно " +
               "и дополнительно смотреть медиану.";
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(8, 9, 3, 0)
    };

    private sealed class CategoryOption
    {
        public int Id { get; }

        private string Text { get; }

        public CategoryOption(int id, string text)
        {
            Id = id;
            Text = text;
        }

        public override string ToString() => Text;
    }
}
