using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Services;
using Serilog;

namespace IncomeExpenseSystem.App.Forms;

/// <summary>Главное окно приложения.</summary>
public sealed class MainForm : Form
{
    private readonly User _user;
    private readonly ServiceContainer _services;

    public MainForm(User user, ServiceContainer services)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "Система анализа доходов и расходов";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        ClientSize = new Size(1150, 720);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var menu = BuildMenu();
        MainMenuStrip = menu;
        root.Controls.Add(menu, 0, 0);

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(22, 16, 22, 10),
            BackColor = Color.FromArgb(240, 244, 250)
        };

        var title = new Label
        {
            Text = "Система анализа доходов и расходов",
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 56, 100)
        };

        var subtitle = new Label
        {
            Text = $"Пользователь: {_user.FullName}   •   роль: {_user.RoleDisplayName}",
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            ForeColor = Color.DimGray
        };

        header.Controls.Add(subtitle);
        header.Controls.Add(title);
        root.Controls.Add(header, 0, 1);

        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(20, 10, 20, 10),
            WrapContents = true
        };

        navigation.Controls.Add(CreateNavButton("Операции", (_, _) => OpenTransactions()));
        navigation.Controls.Add(CreateNavButton("Статистика", (_, _) => OpenStatistics()));
        navigation.Controls.Add(CreateNavButton("Дашборд", (_, _) => OpenDashboard()));

        if (Permissions.CanManageCategories(_user.Role))
            navigation.Controls.Add(CreateNavButton("Категории", (_, _) => OpenCategories()));

        root.Controls.Add(navigation, 0, 2);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            BackColor = Color.White
        };

        var welcome = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12F),
            ForeColor = Color.FromArgb(70, 70, 70),
            Text = "Выберите раздел в меню или на панели выше.\n\n" +
                   "• «Операции» — просмотр, добавление и редактирование доходов и расходов;\n" +
                   "• «Статистика» — среднее, медиана, дисперсия и коэффициент вариации;\n" +
                   "• «Дашборд» — графики доходов, расходов и структуры затрат;\n" +
                   "• «Категории» — управление справочником (только администратор)."
        };

        content.Controls.Add(welcome);
        root.Controls.Add(content, 0, 3);

        var status = new StatusStrip { SizingGrip = false };
        status.Items.Add(new ToolStripStatusLabel($"Вход выполнен: {_user.Login}")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        });
        status.Items.Add(new ToolStripStatusLabel($"Роль: {_user.RoleDisplayName}"));
        root.Controls.Add(status, 0, 4);

        Controls.Add(root);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var file = new ToolStripMenuItem("Файл");
        file.DropDownItems.Add(new ToolStripMenuItem("Выход", null, (_, _) => Close()));

        var references = new ToolStripMenuItem("Справочники");
        var categories = new ToolStripMenuItem("Категории", null, (_, _) => OpenCategories());
        categories.Enabled = Permissions.CanManageCategories(_user.Role);
        references.DropDownItems.Add(categories);

        var operations = new ToolStripMenuItem("Операции");
        operations.DropDownItems.Add(new ToolStripMenuItem("Транзакции", null, (_, _) => OpenTransactions()));

        var analytics = new ToolStripMenuItem("Аналитика");
        analytics.DropDownItems.Add(new ToolStripMenuItem("Статистика", null, (_, _) => OpenStatistics()));
        analytics.DropDownItems.Add(new ToolStripMenuItem("Дашборд", null, (_, _) => OpenDashboard()));

        var help = new ToolStripMenuItem("Справка");
        help.DropDownItems.Add(new ToolStripMenuItem("О программе", null, (_, _) => ShowAbout()));

        menu.Items.AddRange(new ToolStripItem[] { file, references, operations, analytics, help });
        return menu;
    }

    private static Button CreateNavButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(150, 42),
            Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(31, 56, 100),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    private void OpenTransactions()
    {
        OpenForm(() => new TransactionsForm(_user, _services.Transactions, _services.Categories, _services.Reports));
    }

    private void OpenStatistics()
    {
        OpenForm(() => new StatisticsForm(_user, _services.Transactions, _services.Categories, _services.Statistics));
    }

    private void OpenDashboard()
    {
        OpenForm(() => new DashboardForm(_user, _services.Transactions));
    }

    private void OpenCategories()
    {
        if (!Permissions.CanManageCategories(_user.Role))
        {
            MessageBox.Show(this, "Управление категориями доступно только администратору.",
                "Доступ запрещён", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        OpenForm(() => new CategoriesForm(_user, _services.Categories));
    }

    private void OpenForm(Func<Form> formFactory)
    {
        Form? form = null;

        try
        {
            form = formFactory();
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при открытии формы {FormName}", form?.GetType().Name ?? "неизвестной формы");
            MessageBox.Show(this, "Не удалось открыть раздел. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            form?.Dispose();
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(this,
            "Система анализа доходов и расходов\n\n" +
            "Учебный проект на C# (.NET 8, WinForms).\n" +
            "Технологии: SQLite, Dapper, BCrypt, Serilog, ScottPlot, EPPlus, iTextSharp, xUnit.\n\n" +
            "Назначение: учёт операций, расчёт описательных статистик, графики и отчёты.",
            "О программе",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
