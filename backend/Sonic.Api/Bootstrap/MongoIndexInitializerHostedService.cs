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
        // Fail fast if this breaks. Indexes are infra correctness.
        await EnsureUniqueIndexAsync(
            collectionName: "users",
            keysDoc: new BsonDocument { { "Email", 1 } },
            desiredName: "ux_users_email",
            cancellationToken);

        await EnsureUniqueIndexAsync(
            collectionName: "likes",
            keysDoc: new BsonDocument { { "PostId", 1 }, { "UserId", 1 } },
            desiredName: "ux_likes_post_user",
            cancellationToken);

        await EnsureUniqueIndexAsync(
            collectionName: "campaignParticipations",
            keysDoc: new BsonDocument { { "PostId", 1 }, { "UserId", 1 } },
            desiredName: "ux_campaign_participation_post_user",
            cancellationToken);

        _logger.LogInformation("MongoDB indexes ensured successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureUniqueIndexAsync(
        string collectionName,
        BsonDocument keysDoc,
        string desiredName,
        CancellationToken cancellationToken)
    {
        var collection = _dbContext.GetDatabase().GetCollection<BsonDocument>(collectionName);

        var existing = await (await collection.Indexes.ListAsync(cancellationToken)).ToListAsync(cancellationToken);

        foreach (var idx in existing)
        {
            if (!idx.TryGetValue("key", out var keyVal)) continue;

            var existingKeys = keyVal.AsBsonDocument;

            // Same key spec already exists -> do NOT create another index (name can differ)
            if (existingKeys.Equals(keysDoc))
            {
                var isUnique = idx.TryGetValue("unique", out var u) && u.IsBoolean && u.AsBoolean;
                var existingName = idx.TryGetValue("name", out var n) ? n.AsString : "<unknown>";

                if (!isUnique)
                {
                    throw new InvalidOperationException(
                        $"Index exists on {collectionName}({keysDoc}) but is not unique (name='{existingName}').");
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
            new CreateIndexOptions { Unique = true, Name = desiredName });

        await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Created unique index '{Name}' on {Collection}({Keys}).",
            desiredName, collectionName, keysDoc);
    }
}
