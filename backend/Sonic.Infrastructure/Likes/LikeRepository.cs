using MongoDB.Driver;
using Sonic.Application.Likes.interfaces;
using Sonic.Domain.Likes;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Persistence.Documents;

namespace Sonic.Infrastructure.Likes;

public sealed class LikeRepository : ILikeRepository
{
    private readonly IMongoCollection<LikeDocument> _collection;

    public LikeRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<LikeDocument>("likes");
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        // Unique index on (PostId, UserId) to guarantee one like per user per post
        var indexKeys = Builders<LikeDocument>.IndexKeys
            .Ascending(x => x.PostId)
            .Ascending(x => x.UserId);

        var indexModel = new CreateIndexModel<LikeDocument>(
            indexKeys,
            new CreateIndexOptions
            {
                Unique = true,
                Name = "IX_Likes_PostId_UserId"
            });

        _collection.Indexes.CreateOne(indexModel);
    }

    public async Task<bool> ExistsAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId) || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        var filter = Builders<LikeDocument>.Filter.And(
            Builders<LikeDocument>.Filter.Eq(x => x.PostId, postId),
            Builders<LikeDocument>.Filter.Eq(x => x.UserId, userId));

        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task AddAsync(
        Like like,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(like);

        var doc = FromDomain(like);

        try
        {
            await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Like already exists (unique index hit) – make Add idempotent
        }
    }

    public async Task RemoveAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId) || string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var filter = Builders<LikeDocument>.Filter.And(
            Builders<LikeDocument>.Filter.Eq(x => x.PostId, postId),
            Builders<LikeDocument>.Filter.Eq(x => x.UserId, userId));

        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    public async Task<long> CountForPostAsync(
        string postId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            return 0;
        }

        var filter = Builders<LikeDocument>.Filter.Eq(x => x.PostId, postId);
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    // ---------- Mapping ----------

    private static Like ToDomain(LikeDocument doc)
    {
        return Like.FromPersistence(
            id: doc.Id,
            postId: doc.PostId,
            userId: doc.UserId,
            createdAt: doc.CreatedAt);
    }

    private static LikeDocument FromDomain(Like like)
    {
        return new LikeDocument
        {
            Id = like.Id,
            PostId = like.PostId,
            UserId = like.UserId,
            CreatedAt = like.CreatedAt
        };
    }
}
