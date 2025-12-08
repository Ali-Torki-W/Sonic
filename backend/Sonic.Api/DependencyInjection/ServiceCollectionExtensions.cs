using Sonic.Application.Auth.interfaces;
using Sonic.Application.Auth.Services;
using Sonic.Application.Comments.interfaces;
using Sonic.Application.Comments.Services;
using Sonic.Application.Likes.interfaces;
using Sonic.Application.Likes.Services;
using Sonic.Application.Posts.interfaces;
using Sonic.Application.Posts.Services;
using Sonic.Application.Users.interfaces;
using Sonic.Infrastructure.Auth;
using Sonic.Infrastructure.Comments;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Likes;
using Sonic.Infrastructure.Posts;
using Sonic.Infrastructure.Users;

namespace Sonic.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Sonic application + infrastructure services
    /// (Mongo, auth infra, repositories, use-case services, options).
    /// </summary>
    public static IServiceCollection AddSonicCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- Options ----
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDbSettings"));

        // ---- Infrastructure (Mongo, auth, repos) ----
        services.AddSingleton<MongoDbContext>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<ILikeRepository, LikeRepository>();

        // ---- Application services (use-cases) ----
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ILikeService, LikeService>();

        return services;
    }
}
