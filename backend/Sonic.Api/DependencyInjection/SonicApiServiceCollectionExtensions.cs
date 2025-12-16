using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using Sonic.Api.Bootstrap;
using Microsoft.OpenApi;
using Sonic.Api.Config;

namespace Sonic.Api;

public static class SonicApiServiceCollectionExtensions
{
    public static IServiceCollection AddSonicApi(this IServiceCollection services, IConfiguration config)
    {
        // Options
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<MongoDbSettings>(config.GetSection("MongoDbSettings"));
        services.Configure<AdminSeedOptions>(config.GetSection(AdminSeedOptions.SectionName));

        // Infra
        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Repos
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();
        services.AddScoped<ICampaignParticipationRepository, CampaignParticipationRepository>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILikeService, LikeService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IUserService, UserService>();

        // Startup hardening (indexes)
        services.AddHostedService<MongoIndexInitializerHostedService>();

        // Admin bootstrap (seed)
        services.AddHostedService<AdminBootstrapHostedService>();

        // Controllers + JSON
        services.AddControllers().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "Sonic API", Version = "v1" });

            const string schemeId = "bearer";
            o.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Name = "Authorization",
                In = ParameterLocation.Header
            });

            o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(schemeId, document), new List<string>() }
            });
        });

        // Auth
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                          ?? throw new InvalidOperationException("JWT config missing.");

                if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
                    throw new InvalidOperationException("JWT Secret is missing/too short (min 32 chars).");

                var keyBytes = Encoding.UTF8.GetBytes(jwt.Secret);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

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

        services.AddAuthorization(o =>
        {
            o.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
        });

        return services;
    }
}
