using FluentAssertions;
using IncomeExpenseSystem.App.Models;
using IncomeExpenseSystem.App.Repositories;
using IncomeExpenseSystem.App.Services;
using Xunit;

namespace IncomeExpenseSystem.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public void Login_WithValidCredentials_ReturnsUser()
    {
        var user = CreateUser("admin123");
        var service = new AuthService(new FakeUserRepository(user));

        var result = service.Login(" ADMIN ", "admin123");

        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Login.Should().Be("admin");
    }

    [Fact]
    public void Login_WithWrongPassword_ReturnsFailure()
    {
        var user = CreateUser("admin123");
        var service = new AuthService(new FakeUserRepository(user));

        var result = service.Login("admin", "wrong-password");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Неверный логин или пароль");
    }

    [Fact]
    public void Login_WithUnknownLogin_ReturnsFailure()
    {
        var user = CreateUser("admin123");
        var service = new AuthService(new FakeUserRepository(user));

        var result = service.Login("unknown", "admin123");

        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Login_WithInactiveUser_ReturnsFailure()
    {
        var user = CreateUser("admin123");
        user.IsActive = false;
        var service = new AuthService(new FakeUserRepository(user));

        var result = service.Login("admin", "admin123");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("отключена");
    }

    [Fact]
    public void Login_WithEmptyCredentials_ReturnsFailure()
    {
        var service = new AuthService(new FakeUserRepository(null));

        var result = service.Login("", "");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Введите логин и пароль");
    }

    [Fact]
    public void HashPassword_ProducesVerifiableBcryptHash()
    {
        var hash = AuthService.HashPassword("secret");

        hash.Should().StartWith("$2");
        BCrypt.Net.BCrypt.Verify("secret", hash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("another", hash).Should().BeFalse();
    }

    private static User CreateUser(string password) => new()
    {
        Id = 1,
        Login = "admin",
        PasswordHash = AuthService.HashPassword(password),
        FullName = "Администратор системы",
        Role = UserRole.Admin,
        IsActive = true,
        CreatedAt = DateTime.Now
    };

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user)
        {
            _user = user;
        }

        public User? GetByLogin(string login) =>
            _user is not null && string.Equals(_user.Login, login, StringComparison.OrdinalIgnoreCase)
                ? _user
                : null;

        public User? GetById(int id) => _user?.Id == id ? _user : null;

        public IReadOnlyList<User> GetAll() => _user is null ? Array.Empty<User>() : new[] { _user };
    }
}
