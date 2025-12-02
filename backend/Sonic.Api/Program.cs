using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Sonic.Api.MiddleWares;
using Sonic.Application.Auth;
using Sonic.Application.Auth.interfaces;
using Sonic.Application.Auth.Services;
using Sonic.Application.Users;
using Sonic.Domain.Users;
using Sonic.Infrastructure;
using Sonic.Infrastructure.Auth;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

// ---------- Options binding ----------
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// ---------- Core infrastructure ----------
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// ---------- Application services / repositories ----------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ---------- MVC / controllers ----------
builder.Services.AddControllers();

// ---------- Authentication / Authorization ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration section 'Jwt' is missing or invalid.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret is not configured or too short (min 32 chars).");
        }

        var keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Secret);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Keep JWT claim names as-is ("sub", "email", "role", ...)
        options.MapInboundClaims = false;
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();

// ---------- Pipeline ----------
app.UseRouting();

// Global error handling
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// ---------- Health endpoints ----------
app.MapGet("/health", () => Results.Ok(new { status = "OK" }))
   .AllowAnonymous();

app.MapGet("/db-health", async (MongoDbContext dbContext) =>
{
    try
    {
        var command = new BsonDocument("ping", 1);
        await dbContext.GetDatabase().RunCommandAsync<BsonDocument>(command);

        return Results.Ok(new { status = "OK", message = "MongoDB reachable" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"MongoDB health check failed: {ex.Message}");
    }
}).AllowAnonymous();

// ---------- Dummy protected endpoint to test JWT ----------
app.MapGet("/auth-test", (HttpContext httpContext) =>
{
    var user = httpContext.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var userId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                 ?? user.FindFirst("sub")?.Value;

    var email = user.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
    var role = user.FindFirst("role")?.Value
               ?? user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

    return Results.Ok(new
    {
        message = "You are authenticated.",
        userId,
        email,
        role
    });
}).RequireAuthorization();

// ---------- DEV-ONLY: test token endpoint (you can delete this later) ----------
app.MapPost("/dev/token", (IJwtTokenGenerator tokenGenerator) =>
{
    var user = User.CreateNew(
        email: "devuser@sonic.local",
        passwordHash: "ignored-in-this-endpoint",
        displayName: "Dev User",
        role: UserRole.Admin);

    var token = tokenGenerator.GenerateToken(user);

    return Results.Ok(new
    {
        token = token.AccessToken,
        expiresAtUtc = token.ExpiresAtUtc,
        userId = user.Id,
        email = user.Email,
        role = user.Role.ToString()
    });
}).AllowAnonymous();

app.MapControllers();

app.Run();
