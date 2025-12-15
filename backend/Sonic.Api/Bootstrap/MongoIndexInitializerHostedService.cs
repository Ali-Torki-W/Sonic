using MongoDB.Bson;
using MongoDB.Driver;
using Sonic.Infrastructure.Config;

namespace Sonic.Api.Bootstrap;

public sealed class MongoIndexInitializerHostedService(
    MongoDbContext db,
    ILogger<MongoIndexInitializerHostedService> logger) : IHostedService
{
    private readonly MongoDbContext _db = db;
    private readonly ILogger<MongoIndexInitializerHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // IMPORTANT: collection names must match YOUR repositories
        await EnsureUsersIndexes(cancellationToken);
        await EnsureLikesIndexes(cancellationToken);
        await EnsureCampaignParticipationIndexes(cancellationToken);
        await EnsurePostsIndexes(cancellationToken);
        await EnsureCommentsIndexes(cancellationToken);

        _logger.LogInformation("MongoDB indexes ensured.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureUsersIndexes(CancellationToken ct)
    {
        var col = _db.GetCollection<BsonDocument>("users");
        var keys = Builders<BsonDocument>.IndexKeys.Ascending("Email");
        var model = new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Unique = true, Name = "ux_users_email" });
        await col.Indexes.CreateOneAsync(model, cancellationToken: ct);
    }

    private async Task EnsureLikesIndexes(CancellationToken ct)
    {
        var col = _db.GetCollection<BsonDocument>("likes");
        var keys = Builders<BsonDocument>.IndexKeys.Ascending("PostId").Ascending("UserId");
        var model = new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Unique = true, Name = "ux_likes_post_user" });
        await col.Indexes.CreateOneAsync(model, cancellationToken: ct);
    }

    private async Task EnsureCampaignParticipationIndexes(CancellationToken ct)
    {
        var col = _db.GetCollection<BsonDocument>("campaignParticipations");
        var keys = Builders<BsonDocument>.IndexKeys.Ascending("PostId").Ascending("UserId");
        var model = new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Unique = true, Name = "ux_campaign_join_post_user" });
        await col.Indexes.CreateOneAsync(model, cancellationToken: ct);
    }

    private async Task EnsurePostsIndexes(CancellationToken ct)
    {
        var col = _db.GetCollection<BsonDocument>("posts");
        var keys = Builders<BsonDocument>.IndexKeys
            .Ascending("IsDeleted")
            .Descending("CreatedAt");

        var model = new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = "ix_posts_isDeleted_createdAt" });
        await col.Indexes.CreateOneAsync(model, cancellationToken: ct);

        var featuredKeys = Builders<BsonDocument>.IndexKeys
            .Ascending("IsFeatured")
            .Descending("CreatedAt");

        await col.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(featuredKeys, new CreateIndexOptions { Name = "ix_posts_featured_createdAt" }),
            cancellationToken: ct);
    }

    private async Task EnsureCommentsIndexes(CancellationToken ct)
    {
        var col = _db.GetCollection<BsonDocument>("comments");
        var keys = Builders<BsonDocument>.IndexKeys
            .Ascending("PostId")
            .Ascending("IsDeleted")
            .Ascending("CreatedAt");

        var model = new CreateIndexModel<BsonDocument>(keys, new CreateIndexOptions { Name = "ix_comments_post_isDeleted_createdAt" });
        await col.Indexes.CreateOneAsync(model, cancellationToken: ct);
    }
}
