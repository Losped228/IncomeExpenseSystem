using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using IncomeExpenseSystem.App.Services;
using Serilog;

namespace IncomeExpenseSystem.App.Forms;

/// <summary>Справочник категорий доходов и расходов.</summary>
public sealed class CategoriesForm : Form
{
    private readonly User _user;
    private readonly ICategoryRepository _repository;

    private readonly DataGridView _grid = new();
    private readonly TextBox _txtName = new();
    private readonly ComboBox _cmbType = new();
    private readonly TextBox _txtDescription = new();
    private readonly CheckBox _chkActive = new();
    private readonly Button _btnAdd = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnDelete = new();
    private readonly Button _btnClear = new();

    private List<Category> _categories = new();
    private int _selectedId;

    public CategoriesForm(User user, ICategoryRepository repository)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        if (!Permissions.CanManageCategories(_user.Role))
            throw new UnauthorizedAccessException("Управление категориями доступно только администратору.");

        InitializeUi();
        Load += (_, _) => Reload();
    }

    private void InitializeUi()
    {
        Text = "Справочник категорий";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 620);
        ClientSize = new Size(920, 700);
        Font = new Font("Segoe UI", 9.5F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
            DataPropertyName = nameof(Category.Id),
            HeaderText = "ID",
            Visible = false
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Category.Name),
            HeaderText = "Название",
            FillWeight = 180
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Category.TypeDisplayName),
            HeaderText = "Тип",
            FillWeight = 70
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Category.Description),
            HeaderText = "Описание",
            FillWeight = 300
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(Category.ActiveDisplay),
            HeaderText = "Активна",
            FillWeight = 60
        });

        root.Controls.Add(_grid, 0, 0);

        var editor = new GroupBox
        {
            Text = "Карточка категории",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(12)
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(6)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        form.Controls.Add(CreateLabel("Название:"), 0, 0);
        _txtName.Dock = DockStyle.Fill;
        _txtName.MaxLength = 100;
        _txtName.Margin = new Padding(3, 5, 3, 5);
        form.Controls.Add(_txtName, 1, 0);

        form.Controls.Add(CreateLabel("Тип:"), 0, 1);
        _cmbType.Dock = DockStyle.Fill;
        _cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbType.Items.AddRange(new object[] { "Доход", "Расход" });
        _cmbType.SelectedIndex = 0;
        _cmbType.Margin = new Padding(3, 5, 3, 5);
        form.Controls.Add(_cmbType, 1, 1);

        form.Controls.Add(CreateLabel("Описание:"), 0, 2);
        _txtDescription.Dock = DockStyle.Fill;
        _txtDescription.MaxLength = 500;
        _txtDescription.Multiline = true;
        _txtDescription.Height = 70;
        _txtDescription.ScrollBars = ScrollBars.Vertical;
        _txtDescription.Margin = new Padding(3, 5, 3, 5);
        form.Controls.Add(_txtDescription, 1, 2);

        form.Controls.Add(CreateLabel("Активна:"), 0, 3);
        _chkActive.Text = "Категория доступна для выбора в операциях";
        _chkActive.Checked = true;
        _chkActive.AutoSize = true;
        _chkActive.Margin = new Padding(3, 6, 3, 6);
        form.Controls.Add(_chkActive, 1, 3);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 8, 0, 0)
        };

        ConfigureButton(_btnAdd, "Добавить", BtnAdd_Click);
        ConfigureButton(_btnSave, "Сохранить", BtnSave_Click);
        ConfigureButton(_btnDelete, "Удалить", BtnDelete_Click);
        ConfigureButton(_btnClear, "Очистить", (_, _) => ClearEditor());

        var btnClose = new Button { Text = "Закрыть", Size = new Size(110, 32) };
        btnClose.Click += (_, _) => Close();

        buttons.Controls.AddRange(new Control[] { _btnAdd, _btnSave, _btnDelete, _btnClear, btnClose });
        form.Controls.Add(buttons, 0, 4);
        form.SetColumnSpan(buttons, 2);

        editor.Controls.Add(form);
        root.Controls.Add(editor, 0, 1);

        Controls.Add(root);
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
        button.Size = new Size(120, 32);
        button.Margin = new Padding(0, 0, 8, 0);
        button.Click += handler;
    }

    private void Reload()
    {
        try
        {
            _categories = _repository.GetAll(includeInactive: true).ToList();
            _grid.DataSource = null;
            _grid.DataSource = _categories;
            _grid.ClearSelection();
            _selectedId = 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка загрузки категорий");
            MessageBox.Show(this, "Не удалось загрузить список категорий. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow is not { Selected: true } row ||
            row.DataBoundItem is not Category category)
            return;

        _selectedId = category.Id;
        _txtName.Text = category.Name;
        _cmbType.SelectedIndex = category.Type == CategoryType.Income ? 0 : 1;
        _txtDescription.Text = category.Description ?? string.Empty;
        _chkActive.Checked = category.IsActive;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        if (!ValidateInput())
            return;

        try
        {
            _selectedId = 0;
            _repository.Add(BuildCategory());
            Reload();
            ClearEditor();
            MessageBox.Show(this, "Категория добавлена.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка добавления категории");
            MessageBox.Show(this, "Не удалось добавить категорию. Возможно, название уже используется.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_selectedId == 0)
        {
            MessageBox.Show(this, "Выберите категорию в таблице.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidateInput())
            return;

        try
        {
            if (!_repository.Update(BuildCategory()))
            {
                MessageBox.Show(this, "Категория не найдена. Обновите список.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Reload();
                return;
            }

            Reload();
            ClearEditor();
            MessageBox.Show(this, "Изменения сохранены.", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка обновления категории {Id}", _selectedId);
            MessageBox.Show(this, "Не удалось сохранить изменения. Проверьте уникальность названия.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedId == 0)
        {
            MessageBox.Show(this, "Выберите категорию в таблице.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (_repository.HasTransactions(_selectedId))
            {
                MessageBox.Show(this,
                    "Категория используется в операциях. Удаление запрещено, чтобы не нарушить историю учёта.\n" +
                    "Отключите категорию, сняв флажок «Активна».",
                    "Удаление невозможно", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var category = _categories.FirstOrDefault(c => c.Id == _selectedId);
            var name = category?.Name ?? "выбранную категорию";

            if (MessageBox.Show(this, $"Удалить категорию «{name}»?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (_repository.Delete(_selectedId))
            {
                Reload();
                ClearEditor();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка удаления категории {Id}", _selectedId);
            MessageBox.Show(this, "Не удалось удалить категорию. Подробности записаны в журнал.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ValidateInput()
    {
        var name = _txtName.Text.Trim();

        if (name.Length < 2 || name.Length > 100)
        {
            MessageBox.Show(this, "Название категории должно содержать от 2 до 100 символов.",
                "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtName.Focus();
            return false;
        }

        if (_cmbType.SelectedIndex < 0)
        {
            MessageBox.Show(this, "Выберите тип категории.", "Проверка данных",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        try
        {
            var duplicate = _repository.GetByName(name);
            if (duplicate is not null && duplicate.Id != _selectedId)
            {
                MessageBox.Show(this, "Категория с таким названием уже существует.",
                    "Проверка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка проверки уникальности категории");
            MessageBox.Show(this, "Не удалось проверить название категории.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    private Category BuildCategory() => new()
    {
        Id = _selectedId,
        Name = _txtName.Text.Trim(),
        Type = _cmbType.SelectedIndex == 0 ? CategoryType.Income : CategoryType.Expense,
        Description = string.IsNullOrWhiteSpace(_txtDescription.Text) ? null : _txtDescription.Text.Trim(),
        IsActive = _chkActive.Checked
    };

    private void ClearEditor()
    {
        _selectedId = 0;
        _txtName.Clear();
        _cmbType.SelectedIndex = 0;
        _txtDescription.Clear();
        _chkActive.Checked = true;
        _grid.ClearSelection();
        _txtName.Focus();
    }
}
