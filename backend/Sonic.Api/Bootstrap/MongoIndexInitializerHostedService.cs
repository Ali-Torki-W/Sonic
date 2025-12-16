using MongoDB.Bson;
using MongoDB.Driver;
using Sonic.Infrastructure.Config;

namespace Sonic.Api.Bootstrap;

public sealed class MongoIndexInitializerHostedService(
    MongoDbContext dbContext,
    ILogger<MongoIndexInitializerHostedService> logger) : IHostedService
{
    private readonly MongoDbContext _dbContext = dbContext;
    private readonly ILogger<MongoIndexInitializerHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureIndexAsync(
            collectionName: "users",
            keysDoc: new BsonDocument { { "Email", 1 } },
            indexName: "ux_users_email",
            unique: true,
            cancellationToken);

        await EnsureIndexAsync(
            collectionName: "likes",
            keysDoc: new BsonDocument { { "PostId", 1 }, { "UserId", 1 } },
            indexName: "ux_likes_post_user",
            unique: true,
            cancellationToken);

        await EnsureIndexAsync(
            collectionName: "campaignParticipations",
            keysDoc: new BsonDocument { { "PostId", 1 }, { "UserId", 1 } },
            indexName: "ux_campaign_participation_post_user",
            unique: true,
            cancellationToken);

        _logger.LogInformation("MongoDB indexes ensured.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureIndexAsync(
        string collectionName,
        BsonDocument keysDoc,
        string indexName,
        bool unique,
        CancellationToken cancellationToken)
    {
        var collection = _dbContext.GetDatabase().GetCollection<BsonDocument>(collectionName);

        var existing = await (await collection.Indexes.ListAsync(cancellationToken))
            .ToListAsync(cancellationToken);

        foreach (var idx in existing)
        {
            if (!idx.TryGetValue("key", out var keyVal) || keyVal.BsonType != BsonType.Document)
                continue;

            var existingKeys = keyVal.AsBsonDocument;

            // Same key spec exists => already ensured (name irrelevant)
            if (existingKeys.Equals(keysDoc))
            {
                var existingName = idx.TryGetValue("name", out var n) ? n.AsString : "<unknown>";
                var existingUnique = idx.TryGetValue("unique", out var u) && u.IsBoolean && u.AsBoolean;

                if (unique && !existingUnique)
                {
                    throw new InvalidOperationException(
                        $"Index exists on {collectionName}({keysDoc}) but is not unique (existing='{existingName}').");
                }

                _logger.LogInformation(
                    "Index already exists on {Collection}({Keys}) as '{ExistingName}'. Skipping.",
                    collectionName, keysDoc, existingName);

                return;
            }
        }

        var keysDef = new BsonDocumentIndexKeysDefinition<BsonDocument>(keysDoc);
        var model = new CreateIndexModel<BsonDocument>(
            keysDef,
            new CreateIndexOptions { Name = indexName, Unique = unique });

        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);
            _logger.LogInformation("Created index '{Name}' on {Collection}({Keys}).", indexName, collectionName, keysDoc);
        }
        catch (MongoCommandException ex) when (
            ex.Message.Contains("Index already exists with a different name", StringComparison.OrdinalIgnoreCase))
        {
            // Safety net if something created the same keys concurrently / previously with different name.
            _logger.LogWarning(
                "Index already exists on {Collection}({Keys}) under a different name. Skipping creation.",
                collectionName, keysDoc);
        }
    }
}
