using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Services;
using Serilog;

namespace IncomeExpenseSystem.App.Forms;

/// <summary>Форма входа в систему.</summary>
public sealed class LoginForm : Form
{
    private readonly AuthService _authService;

    private readonly TextBox _txtLogin = new();
    private readonly TextBox _txtPassword = new();
    private readonly Button _btnLogin = new();
    private readonly Button _btnExit = new();
    private readonly Label _lblError = new();

    private readonly System.Windows.Forms.Timer _lockTimer = new() { Interval = 1000 };

    private int _failedAttempts;
    private DateTime _lockoutUntil;

    public User? CurrentUser { get; private set; }

    public LoginForm(AuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        InitializeUi();
        _lockTimer.Tick += LockTimer_Tick;
    }

    private void InitializeUi()
    {
        Text = "Вход в систему";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(470, 310);
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(28, 20, 28, 18)
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            Text = "Анализ доходов и расходов",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 56, 100)
        };
        root.Controls.Add(title, 0, 0);
        root.SetColumnSpan(title, 2);

        var loginLabel = new Label
        {
            Text = "Логин:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(loginLabel, 0, 1);

        _txtLogin.Dock = DockStyle.Fill;
        _txtLogin.Margin = new Padding(3, 7, 3, 7);
        _txtLogin.PlaceholderText = "Введите логин";
        _txtLogin.MaxLength = 50;
        root.Controls.Add(_txtLogin, 1, 1);

        var passwordLabel = new Label
        {
            Text = "Пароль:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(passwordLabel, 0, 2);

        _txtPassword.Dock = DockStyle.Fill;
        _txtPassword.Margin = new Padding(3, 7, 3, 7);
        _txtPassword.PlaceholderText = "Введите пароль";
        _txtPassword.UseSystemPasswordChar = true;
        _txtPassword.MaxLength = 100;
        root.Controls.Add(_txtPassword, 1, 2);

        _lblError.Dock = DockStyle.Fill;
        _lblError.TextAlign = ContentAlignment.MiddleLeft;
        _lblError.ForeColor = Color.Firebrick;
        _lblError.Font = new Font("Segoe UI", 9F);
        root.Controls.Add(_lblError, 0, 3);
        root.SetColumnSpan(_lblError, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        _btnLogin.Text = "Войти";
        _btnLogin.Size = new Size(120, 34);
        _btnLogin.Click += BtnLogin_Click;

        _btnExit.Text = "Выход";
        _btnExit.Size = new Size(120, 34);
        _btnExit.Click += BtnExit_Click;

        buttons.Controls.Add(_btnLogin);
        buttons.Controls.Add(_btnExit);
        root.Controls.Add(buttons, 0, 4);
        root.SetColumnSpan(buttons, 2);

        var hint = new Label
        {
           
        };
        root.Controls.Add(hint, 0, 5);
        root.SetColumnSpan(hint, 2);

        Controls.Add(root);
        AcceptButton = _btnLogin;
        CancelButton = _btnExit;

        Shown += (_, _) => _txtLogin.Focus();
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        if (DateTime.Now < _lockoutUntil)
            return;

        _lblError.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(_txtLogin.Text) || string.IsNullOrWhiteSpace(_txtPassword.Text))
        {
            _lblError.Text = "Введите логин и пароль.";
            return;
        }

        try
        {
            var result = _authService.Login(_txtLogin.Text, _txtPassword.Text);

            if (!result.Success)
            {
                _failedAttempts++;
                _lblError.Text = result.Message;

                if (_failedAttempts >= 3)
                {
                    _lockoutUntil = DateTime.Now.AddSeconds(30);
                    _btnLogin.Enabled = false;
                    _lockTimer.Start();
                }

                _txtPassword.Clear();
                _txtPassword.Focus();
                return;
            }

            CurrentUser = result.User;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Непредвиденная ошибка при входе");
            MessageBox.Show(this, "Произошла ошибка при входе. Попробуйте ещё раз.",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnExit_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void LockTimer_Tick(object? sender, EventArgs e)
    {
        var secondsLeft = (int)Math.Ceiling((_lockoutUntil - DateTime.Now).TotalSeconds);

        if (secondsLeft <= 0)
        {
            _lockTimer.Stop();
            _btnLogin.Enabled = true;
            _failedAttempts = 0;
            _lblError.Text = string.Empty;
            return;
        }

        _lblError.Text = $"Слишком много попыток. Повторите через {secondsLeft} с.";
    }
}
