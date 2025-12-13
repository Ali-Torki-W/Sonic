using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Bson;
using Sonic.Api.DependencyInjection;
using Sonic.Api.MiddleWares;
using Sonic.Infrastructure.Auth;
using Sonic.Infrastructure.Config;

var builder = WebApplication.CreateBuilder(args);

// ---------- Sonic core DI (Application + Infrastructure + options) ----------
builder.Services.AddSonicCore(builder.Configuration);

// ---------- Error handling middleware ----------
builder.Services.AddTransient<ErrorHandlingMiddleware>();

// ---------- MVC / controllers + JSON ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---------- Swagger / OpenAPI ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sonic API",
        Version = "v1",
        Description = "Sonic – AI experience sharing platform (MVP)"
    });

    // IMPORTANT: schemeId must match the reference below.
    const string schemeId = "bearer";

    options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    // IMPORTANT: v10 uses OpenApiSecuritySchemeReference and expects List<string>
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(schemeId, document),
            new List<string>()
        }
    });

    // Optional: include XML comments if enabled
    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

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
            ClockSkew = TimeSpan.FromMinutes(1),

            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Sub
        };

        options.MapInboundClaims = false;
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// ---------- Pipeline ----------
app.UseRouting();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sonic API v1");
    c.RoutePrefix = "swagger";
});

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

// ---------- JWT test endpoint ----------
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

    return Results.Ok(new { message = "You are authenticated.", userId, email, role });
}).RequireAuthorization();

app.MapControllers();

app.Run();
