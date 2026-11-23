namespace IncomeExpenseSystem.App.Models;

/// <summary>
/// Роли пользователей системы.
/// </summary>
public enum UserRole
{
    Admin,
    Accountant,
    Analyst
}

/// <summary>
/// Учётная запись пользователя.
/// </summary>
public sealed class User
{
    public int Id { get; set; }

    public string Login { get; set; } = string.Empty;

    /// <summary>Хэш пароля BCrypt. Открытый пароль в базе не хранится.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public string RoleDisplayName => Role switch
    {
        UserRole.Admin => "Администратор",
        UserRole.Accountant => "Бухгалтер",
        UserRole.Analyst => "Аналитик",
        _ => Role.ToString()
    };
}
