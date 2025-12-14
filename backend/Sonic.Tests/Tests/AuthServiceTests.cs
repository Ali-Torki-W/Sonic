using Microsoft.Extensions.Options;
using Sonic.Application.Auth.DTOs;
using Sonic.Application.Auth.Services;
using Sonic.Infrastructure.Auth;
using Sonic.Tests.Fakes;
using Sonic.Tests.TestHelpers;

namespace Sonic.Tests.Tests;

public sealed class AuthServiceTests
{
    private static JwtTokenGenerator BuildJwtGen()
    {
        // Any 32+ secret is fine for tests.
        var opts = Options.Create(new JwtOptions
        {
            Issuer = "sonic-tests",
            Audience = "sonic-tests",
            Secret = "THIS_IS_A_TEST_SECRET_32_CHARS_MINIMUM!!",
            AccessTokenMinutes = 60
        });

        return new JwtTokenGenerator(opts);
    }

    [Fact]
    public async Task Register_Succeeds()
    {
        var repo = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var jwt = BuildJwtGen();

        var svc = new AuthService(repo, hasher, jwt);

        var res = await svc.RegisterAsync(new RegisterRequest
        {
            Email = "a@b.com",
            Password = "P@ssw0rd!",
            DisplayName = "Ali"
        });

        Assert.False(string.IsNullOrWhiteSpace(res.UserId));
        Assert.Equal("a@b.com", res.Email);
        Assert.Equal("Ali", res.DisplayName);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var repo = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var jwt = BuildJwtGen();
        var svc = new AuthService(repo, hasher, jwt);

        await svc.RegisterAsync(new RegisterRequest
        {
            Email = "dup@b.com",
            Password = "P@ssw0rd!",
            DisplayName = "User1"
        });

        await ExceptionAssert.AssertStatusCodeAsync(
            () => svc.RegisterAsync(new RegisterRequest
            {
                Email = "dup@b.com",
                Password = "P@ssw0rd!",
                DisplayName = "User2"
            }),
            expectedStatusCode: 409);
    }

    [Fact]
    public async Task Login_Succeeds_ReturnsToken()
    {
        var repo = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var jwt = BuildJwtGen();
        var svc = new AuthService(repo, hasher, jwt);

        await svc.RegisterAsync(new RegisterRequest
        {
            Email = "login@b.com",
            Password = "P@ssw0rd!",
            DisplayName = "LoginUser"
        });

        var res = await svc.LoginAsync(new LoginRequest
        {
            Email = "login@b.com",
            Password = "P@ssw0rd!"
        });

        Assert.False(string.IsNullOrWhiteSpace(res.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(res.UserId));
        Assert.Equal("login@b.com", res.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var repo = new InMemoryUserRepository();
        var hasher = new TestPasswordHasher();
        var jwt = BuildJwtGen();
        var svc = new AuthService(repo, hasher, jwt);

        await svc.RegisterAsync(new RegisterRequest
        {
            Email = "badpass@b.com",
            Password = "P@ssw0rd!",
            DisplayName = "BadPass"
        });

        await ExceptionAssert.AssertStatusCodeAsync(
            () => svc.LoginAsync(new LoginRequest
            {
                Email = "badpass@b.com",
                Password = "WRONG"
            }),
            expectedStatusCode: 401);
    }
}

// Deterministic hasher for tests (fast + stable)
file sealed class TestPasswordHasher : Application.Auth.interfaces.IPasswordHasher
{
    public string Hash(string password) => $"hash::{password}";
    public bool Verify(string password, string passwordHash) => passwordHash == $"hash::{password}";
}
