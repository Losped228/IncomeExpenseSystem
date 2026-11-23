using IncomeExpenseSystem.App.Database;
using IncomeExpenseSystem.App.Forms;
using IncomeExpenseSystem.App.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace IncomeExpenseSystem.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);

            var levelText = configuration["Serilog:MinimumLevel"] ?? "Information";
            var level = Enum.TryParse<LogEventLevel>(levelText, ignoreCase: true, out var parsed)
                ? parsed
                : LogEventLevel.Information;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.File(
                    Path.Combine(logDirectory, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Application.ThreadException += (_, e) =>
            {
                Log.Error(e.Exception, "Необработанная ошибка интерфейса");
                MessageBox.Show("Произошла непредвиденная ошибка. Подробности записаны в журнал.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                    Log.Fatal(exception, "Необработанная ошибка приложения");
                else
                    Log.Fatal("Необработанная ошибка приложения: {Error}", e.ExceptionObject);

                Log.CloseAndFlush();
            };

            Log.Information("Запуск приложения");

            var rawConnectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("В appsettings.json не задана строка подключения Default.");

            var connectionBuilder = new SqliteConnectionStringBuilder(rawConnectionString);
            if (!Path.IsPathRooted(connectionBuilder.DataSource))
            {
                connectionBuilder.DataSource = Path.Combine(
                    AppContext.BaseDirectory,
                    connectionBuilder.DataSource);
            }

            var databaseDirectory = Path.GetDirectoryName(connectionBuilder.DataSource);
            if (!string.IsNullOrWhiteSpace(databaseDirectory))
                Directory.CreateDirectory(databaseDirectory);

            var factory = new SqliteConnectionFactory(connectionBuilder.ToString());
            DatabaseHelper.Initialize(factory);

            var services = new ServiceContainer(factory);

            using var loginForm = new LoginForm(services.Auth);
            if (loginForm.ShowDialog() != DialogResult.OK || loginForm.CurrentUser is null)
            {
                Log.Information("Вход отменён пользователем");
                return;
            }

            Application.Run(new MainForm(loginForm.CurrentUser, services));
            Log.Information("Приложение завершено");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Критическая ошибка при запуске приложения");
            MessageBox.Show("Не удалось запустить приложение. Подробности записаны в журнал.\n\n" + ex.Message,
                "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
