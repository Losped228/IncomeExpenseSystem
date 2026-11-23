using IncomeExpenseSystem.App.Models;

namespace IncomeExpenseSystem.App.Services;

/// <summary>
/// Матрица прав доступа. Проверки выполняются и в интерфейсе,
/// и в сервисах/репозиториях, чтобы ограничения нельзя было обойти.
/// </summary>
public static class Permissions
{
    public static bool CanManageCategories(UserRole role) => role == UserRole.Admin;

    public static bool CanManageTransactions(UserRole role) =>
        role is UserRole.Admin or UserRole.Accountant;

    public static bool CanViewTransactions(UserRole role) => true;

    public static bool CanViewStatistics(UserRole role) => true;

    public static bool CanExport(UserRole role) => true;

    public static string RoleName(UserRole role) => role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Accountant => "Бухгалтер",
        UserRole.Analyst => "Аналитик",
        _ => role.ToString()
    };
}
