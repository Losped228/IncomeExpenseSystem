using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using Serilog;

namespace IncomeExpenseSystem.App.Services;

/// <summary>
/// Аутентификация пользователей и работа с паролями.
/// Открытые пароли не хранятся: в базе лежит только BCrypt-хэш.
/// </summary>
public sealed class AuthService
{
    private readonly IUserRepository _users;

    public AuthService(IUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    /// <summary>
    /// Проверяет логин и пароль. Возвращает понятный пользователю результат
    /// без раскрытия информации о том, существует ли логин.
    /// </summary>
    public AuthResult Login(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Введите логин и пароль.");

        try
        {
            var user = _users.GetByLogin(login.Trim());

            if (user is null)
            {
                Log.Warning("Неудачная попытка входа: логин {Login} не найден", login);
                return AuthResult.Fail("Неверный логин или пароль.");
            }

            if (!user.IsActive)
            {
                Log.Warning("Попытка входа заблокированного пользователя {Login}", login);
                return AuthResult.Fail("Учётная запись отключена администратором.");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                Log.Warning("Неверный пароль для пользователя {Login}", login);
                return AuthResult.Fail("Неверный логин или пароль.");
            }

            Log.Information("Пользователь {Login} вошёл в систему, роль {Role}", user.Login, user.Role);
            return AuthResult.Ok(user);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка аутентификации пользователя {Login}", login);
            return AuthResult.Fail("Не удалось подключиться к базе данных. Попробуйте позже.");
        }
    }

    /// <summary>Создаёт BCrypt-хэш пароля (work factor 12).</summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Пароль не может быть пустым.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
}
