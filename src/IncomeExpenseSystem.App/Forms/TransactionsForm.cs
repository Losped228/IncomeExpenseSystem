using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using IncomeExpenseSystem.App.Services;
using Serilog;

namespace IncomeExpenseSystem.App.Forms;

/// <summary>Просмотр и редактирование операций, экспорт отчётов.</summary>
public sealed class TransactionsForm : Form
{
    private readonly User _user;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ReportService _reportService;

    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly ComboBox _cmbFilterCategory = new();
    private readonly ComboBox _cmbFilterType = new();
    private readonly TextBox _txtSearch = new();
    private readonly Label _lblStatus = new();

    private readonly DataGridView _grid = new();

    private readonly DateTimePicker _dtDate = new();
    private readonly ComboBox _cmbEditCategory = new();
    private readonly NumericUpDown _numAmount = new();
    private readonly TextBox _txtComment = new();

    private readonly Button _btnAdd = new();
    private readonly Button _btnUpdate = new();
    private readonly Button _btnDelete = new();

    private readonly bool _canEdit;
    private int _selectedId;

    public TransactionsForm(
        User user,
        ITransactionRepository transactionRepository,
        ICategoryRepository categoryRepository,
        ReportService reportService)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _canEdit = Permissions.CanManageTransactions(_user.Role);

        InitializeUi();
        Load += (_, _) =>
        {
            LoadCategories();
            ClearEditor();
            ApplyFilter();
        };
    }

    private void InitializeUi()
    {
        Text = "Операции";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(950, 650);
        ClientSize = new Size(1100, 760);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildFilters(), 0, 0);
        root.Controls.Add(BuildGrid(), 0, 1);
        root.Controls.Add(BuildEditor(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildFilters()
    {
        var group = new GroupBox
        {
            Text = "Фильтр",
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
        _from.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _from.Width = 110;
        _from.Margin = new Padding(3, 5, 12, 5);

        _to.Format = DateTimePickerFormat.Short;
        _to.Value = DateTime.Today;
        _to.Width = 110;
        _to.Margin = new Padding(3, 5, 12, 5);

        _cmbFilterCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbFilterCategory.Width = 230;
        _cmbFilterCategory.Margin = new Padding(3, 5, 12, 5);

        _cmbFilterType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbFilterType.Width = 130;
        _cmbFilterType.Margin = new Padding(3, 5, 12, 5);
        _cmbFilterType.Items.AddRange(new object[] { "Все типы", "Доходы", "Расходы" });
        _cmbFilterType.SelectedIndex = 0;

        _txtSearch.Width = 190;
        _txtSearch.PlaceholderText = "Поиск по комментарию/категории";
        _txtSearch.Margin = new Padding(3, 5, 12, 5);

        panel.Controls.Add(CreateLabel("С даты:"));
        panel.Controls.Add(_from);
        panel.Controls.Add(CreateLabel("По дату:"));
        panel.Controls.Add(_to);
        panel.Controls.Add(CreateLabel("Категория:"));
        panel.Controls.Add(_cmbFilterCategory);
        panel.Controls.Add(CreateLabel("Тип:"));
        panel.Controls.Add(_cmbFilterType);
        panel.Controls.Add(CreateLabel("Поиск:"));
        panel.Controls.Add(_txtSearch);

        var btnApply = new Button { Text = "Применить", Size = new Size(110, 30), Margin = new Padding(0, 4, 8, 4) };
        btnApply.Click += (_, _) => ApplyFilter();

        var btnReset = new Button { Text = "Сбросить", Size = new Size(110, 30), Margin = new Padding(0, 4, 8, 4) };
        btnReset.Click += (_, _) =>
        {
            _from.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _to.Value = DateTime.Today;
            _cmbFilterType.SelectedIndex = 0;
            _txtSearch.Clear();
            if (_cmbFilterCategory.Items.Count > 0)
                _cmbFilterCategory.SelectedIndex = 0;
            ApplyFilter();
        };

        var btnExcel = new Button { Text = "Экспорт Excel", Size = new Size(125, 30), Margin = new Padding(0, 4, 8, 4) };
        btnExcel.Click += (_, _) => ExportReport(toPdf: false);

        var btnPdf = new Button { Text = "Экспорт PDF", Size = new Size(115, 30), Margin = new Padding(0, 4, 8, 4) };
        btnPdf.Click += (_, _) => ExportReport(toPdf: true);

        panel.Controls.Add(btnApply);
        panel.Controls.Add(btnReset);
        panel.Controls.Add(btnExcel);
        panel.Controls.Add(btnPdf);

        _lblStatus.AutoSize = true;
        _lblStatus.ForeColor = Color.DimGray;
        _lblStatus.Margin = new Padding(8, 11, 0, 4);
        panel.Controls.Add(_lblStatus);

        group.Controls.Add(panel);
        return group;
    }

    private Control BuildGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(219, 229, 241);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        _grid.SelectionChanged += Grid_SelectionChanged;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.Id),
            HeaderText = "ID",
            Visible = false
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.TxDate),
            HeaderText = "Дата",
            FillWeight = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.CategoryName),
            HeaderText = "Категория",
            FillWeight = 170
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.TypeDisplayName),
            HeaderText = "Тип",
            FillWeight = 65
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.Amount),
            HeaderText = "Сумма, руб.",
            FillWeight = 100,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.UserName),
            HeaderText = "Автор",
            FillWeight = 150
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(TransactionView.Comment),
            HeaderText = "Комментарий",
            FillWeight = 260
        });

        return _grid;
    }

    private Control BuildEditor()
    {
        var group = new GroupBox
        {
            Text = _canEdit ? "Карточка операции" : "Просмотр операции (роль «Аналитик» — только чтение)",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 3,
            Padding = new Padding(4)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateLabel("Дата:"), 0, 0);
        _dtDate.Format = DateTimePickerFormat.Short;
        _dtDate.Value = DateTime.Today;
        _dtDate.Dock = DockStyle.Fill;
        _dtDate.Margin = new Padding(3, 5, 12, 5);
        layout.Controls.Add(_dtDate, 1, 0);

        layout.Controls.Add(CreateLabel("Категория:"), 2, 0);
        _cmbEditCategory.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbEditCategory.Dock = DockStyle.Fill;
        _cmbEditCategory.Margin = new Padding(3, 5, 3, 5);
        layout.Controls.Add(_cmbEditCategory, 3, 0);

        layout.Controls.Add(CreateLabel("Сумма, руб.:"), 0, 1);
        _numAmount.DecimalPlaces = 2;
        _numAmount.Minimum = 0m;
        _numAmount.Maximum = 1_000_000_000m;
        _numAmount.Increment = 100m;
        _numAmount.ThousandsSeparator = true;
        _numAmount.Dock = DockStyle.Fill;
        _numAmount.Margin = new Padding(3, 5, 12, 5);
        layout.Controls.Add(_numAmount, 1, 1);

        layout.Controls.Add(CreateLabel("Комментарий:"), 2, 1);
        _txtComment.MaxLength = 500;
        _txtComment.Dock = DockStyle.Fill;
        _txtComment.Margin = new Padding(3, 5, 3, 5);
        layout.Controls.Add(_txtComment, 3, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 8, 0, 0)
        };

        ConfigureButton(_btnAdd, "Добавить", BtnAdd_Click);
        ConfigureButton(_btnUpdate, "Сохранить", BtnUpdate_Click);
        ConfigureButton(_btnDelete, "Удалить", BtnDelete_Click);

        buttons.Controls.AddRange(new Control[] { _btnAdd, _btnUpdate, _btnDelete });
        ConfigureButton(buttons, "Очистить", (_, _) => ClearEditor());
        ConfigureButton(buttons, "Закрыть", (_, _) => Close());

        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 4);

        if (!_canEdit)
        {
            _dtDate.Enabled = false;
            _cmbEditCategory.Enabled = false;
            _numAmount.Enabled = false;
            _txtComment.Enabled = false;
            _btnAdd.Enabled = false;
            _btnUpdate.Enabled = false;
            _btnDelete.Enabled = false;
        }

        group.Controls.Add(layout);
        return group;
    }

    private static Label CreateLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoSize = false
    };

    private static void ConfigureButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Size = new Size(115, 32);
        button.Margin = new Padding(0, 0, 8, 0);
        button.Click += handler;
    }

    private static void ConfigureButton(Control parent, string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(115, 32),
            Margin = new Padding(0, 0, 8, 0)
        };
        button.Click += handler;
        parent.Controls.Add(button);
    }

    private void LoadCategories()
    {
        try
        {
            var categories = _categoryRepository.GetAll().ToList();

            _cmbFilterCategory.Items.Clear();
            _cmbFilterCategory.Items.Add(new CategoryOption(0, "Все категории"));

            _cmbEditCategory.Items.Clear();

            foreach (var category in categories)
            {
                var label = $"{category.Name} ({category.TypeDisplayName})";
                _cmbFilterCategory.Items.Add(new CategoryOption(category.Id, label));
                _cmbEditCategory.Items.Add(new CategoryOption(category.Id, label));
            }

            _cmbFilterCategory.SelectedIndex = 0;
            if (_cmbEditCategory.Items.Count > 0)
                _cmbEditCategory.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки категорий для формы операций");
            MessageBox.Show(this, "Не удалось загрузить категории. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyFilter()
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
                CategoryId = GetFilterCategoryId(),
                Type = GetFilterType(),
                Search = _txtSearch.Text.Trim(),
                Limit = 10_000
            };

            var rows = _transactionRepository.GetFiltered(filter);
            _grid.DataSource = null;
            _grid.DataSource = rows;
            _lblStatus.Text = $"Найдено операций: {rows.Count}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки операций");
            MessageBox.Show(this, "Не удалось загрузить операции. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private int? GetFilterCategoryId()
    {
        return _cmbFilterCategory.SelectedItem is CategoryOption option && option.Id > 0
            ? option.Id
            : null;
    }

    private CategoryType? GetFilterType()
    {
        return _cmbFilterType.SelectedIndex switch
        {
            1 => CategoryType.Income,
            2 => CategoryType.Expense,
            _ => null
        };
    }

    private int GetEditCategoryId()
    {
        return _cmbEditCategory.SelectedItem is CategoryOption option ? option.Id : 0;
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow is not { Selected: true } row ||
            row.DataBoundItem is not TransactionView item)
            return;

        _selectedId = item.Id;
        _dtDate.Value = item.TxDate.Date;

        for (var i = 0; i < _cmbEditCategory.Items.Count; i++)
        {
            if (_cmbEditCategory.Items[i] is CategoryOption option && option.Id == item.CategoryId)
            {
                _cmbEditCategory.SelectedIndex = i;
                break;
            }
        }

        _numAmount.Value = Math.Clamp(item.Amount, _numAmount.Minimum, _numAmount.Maximum);
        _txtComment.Text = item.Comment ?? string.Empty;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (!ValidateInput())
            return;

        try
        {
            var transaction = new Transaction
            {
                UserId = _user.Id,
                CategoryId = GetEditCategoryId(),
                TxDate = _dtDate.Value.Date,
                Amount = _numAmount.Value,
                Comment = string.IsNullOrWhiteSpace(_txtComment.Text) ? null : _txtComment.Text.Trim()
            };

            _transactionRepository.Add(transaction);
            ApplyFilter();
            ClearEditor();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка добавления операции");
            MessageBox.Show(this, "Не удалось добавить операцию. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnUpdate_Click(object? sender, EventArgs e)
    {
        if (_selectedId == 0)
        {
            MessageBox.Show(this, "Выберите операцию в таблице.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidateInput())
            return;

        try
        {
            var transaction = new Transaction
            {
                Id = _selectedId,
                UserId = _user.Id,
                CategoryId = GetEditCategoryId(),
                TxDate = _dtDate.Value.Date,
                Amount = _numAmount.Value,
                Comment = string.IsNullOrWhiteSpace(_txtComment.Text) ? null : _txtComment.Text.Trim()
            };

            if (!_transactionRepository.Update(transaction))
            {
                MessageBox.Show(this, "Операция не найдена. Обновите список.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ApplyFilter();
                return;
            }

            ApplyFilter();
            ClearEditor();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка обновления операции {Id}", _selectedId);
            MessageBox.Show(this, "Не удалось сохранить изменения. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedId == 0)
        {
            MessageBox.Show(this, "Выберите операцию в таблице.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this, "Удалить выбранную операцию?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            if (_transactionRepository.Delete(_selectedId))
            {
                ApplyFilter();
                ClearEditor();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка удаления операции {Id}", _selectedId);
            MessageBox.Show(this, "Не удалось удалить операцию. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ValidateInput()
    {
        if (GetEditCategoryId() <= 0)
        {
            MessageBox.Show(this, "Выберите категорию операции.", "Проверка данных",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _cmbEditCategory.Focus();
            return false;
        }

        if (_numAmount.Value <= 0m)
        {
            MessageBox.Show(this, "Сумма должна быть больше нуля.", "Проверка данных",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _numAmount.Focus();
            return false;
        }

        if (_dtDate.Value.Date > DateTime.Today.AddDays(1))
        {
            MessageBox.Show(this, "Дата операции не может быть в будущем.", "Проверка данных",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _dtDate.Focus();
            return false;
        }

        return true;
    }

    private void ClearEditor()
    {
        _selectedId = 0;
        _dtDate.Value = DateTime.Today;
        _numAmount.Value = 0m;
        _txtComment.Clear();

        if (_cmbEditCategory.Items.Count > 0)
            _cmbEditCategory.SelectedIndex = 0;

        _grid.ClearSelection();
    }

    private void ExportReport(bool toPdf)
    {
        try
        {
            if (_to.Value.Date < _from.Value.Date)
            {
                MessageBox.Show(this, "Дата окончания не может быть раньше даты начала.",
                    "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var report = _reportService.BuildReport(
                _from.Value.Date,
                _to.Value.Date,
                GetFilterCategoryId(),
                GetFilterType(),
                _user.FullName);

            using var dialog = new SaveFileDialog
            {
                Title = "Сохранить отчёт",
                Filter = toPdf ? "PDF-файл (*.pdf)|*.pdf" : "Книга Excel (*.xlsx)|*.xlsx",
                FileName = $"Отчет_{DateTime.Now:yyyyMMdd_HHmm}.{(toPdf ? "pdf" : "xlsx")}",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (toPdf)
                _reportService.ExportToPdf(report, dialog.FileName);
            else
                _reportService.ExportToExcel(report, dialog.FileName);

            MessageBox.Show(this, "Отчёт сохранён:\n" + dialog.FileName, "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка экспорта отчёта");
            MessageBox.Show(this, "Не удалось сформировать отчёт. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

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
