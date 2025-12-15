using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Bson;
using Sonic.Api.Bootstrap;
using Sonic.Api.MiddleWares;
using Sonic.Application.Auth.interfaces;
using Sonic.Application.Auth.Services;
using Sonic.Application.Campaigns.interfaces;
using Sonic.Application.Campaigns.Services;
using Sonic.Application.Comments.interfaces;
using Sonic.Application.Comments.Services;
using Sonic.Application.Likes.interfaces;
using Sonic.Application.Likes.Services;
using Sonic.Application.Posts.interfaces;
using Sonic.Application.Posts.Services;
using Sonic.Application.Users.interfaces;
using Sonic.Application.Users.Services;
using Sonic.Infrastructure.Auth;
using Sonic.Infrastructure.Campaigns;
using Sonic.Infrastructure.Comments;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Likes;
using Sonic.Infrastructure.Posts;
using Sonic.Infrastructure.Users;

var builder = WebApplication.CreateBuilder(args);

// Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

// Infra
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

// Repos
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ILikeRepository, LikeRepository>();
builder.Services.AddScoped<ICampaignParticipationRepository, CampaignParticipationRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IUserService, UserService>();

// Startup hardening
builder.Services.AddHostedService<MongoIndexInitializerHostedService>();

// Controllers + JSON
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sonic API",
        Version = "v1"
    });

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

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference(schemeId, document), new List<string>() }
    });

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                        ?? throw new InvalidOperationException("JWT config missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret) || jwtOptions.Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret is missing/too short (min 32 chars).");
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

app.UseRouting();

// Error handling FIRST (after routing)
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sonic API v1");
    c.RoutePrefix = "swagger";
});

app.MapGet("/health", () => Results.Ok(new { status = "OK" })).AllowAnonymous();

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

app.MapControllers();

app.Run();
