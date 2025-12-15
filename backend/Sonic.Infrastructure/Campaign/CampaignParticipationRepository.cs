using MongoDB.Driver;
using Sonic.Application.Campaigns.interfaces;
using Sonic.Domain.Campaigns;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Persistence.Documents;

namespace Sonic.Infrastructure.Campaigns;

public sealed class CampaignParticipationRepository : ICampaignParticipationRepository
{
    private readonly IMongoCollection<CampaignParticipationDocument> _collection;

    public CampaignParticipationRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<CampaignParticipationDocument>("campaignParticipants");
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        // Unique (PostId, UserId) so a user can join a campaign at most once
        var indexKeys = Builders<CampaignParticipationDocument>.IndexKeys
            .Ascending(x => x.PostId)
            .Ascending(x => x.UserId);

        var indexModel = new CreateIndexModel<CampaignParticipationDocument>(
            indexKeys,
            new CreateIndexOptions
            {
                Unique = true,
                Name = "IX_CampaignParticipation_PostId_UserId"
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

        var filter = Builders<CampaignParticipationDocument>.Filter.And(
            Builders<CampaignParticipationDocument>.Filter.Eq(x => x.PostId, postId),
            Builders<CampaignParticipationDocument>.Filter.Eq(x => x.UserId, userId));

        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task AddAsync(
        CampaignParticipation participation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(participation);

        var doc = FromDomain(participation);

        try
        {
            await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Already joined -> idempotent
        }
    }

    public async Task<long> CountForPostAsync(
        string postId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            return 0;
        }

        var filter = Builders<CampaignParticipationDocument>.Filter.Eq(x => x.PostId, postId);
        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<CampaignParticipation>> GetByPostIdAsync(
        string postId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            return Array.Empty<CampaignParticipation>();
        }

        var filter = Builders<CampaignParticipationDocument>.Filter.Eq(x => x.PostId, postId);

        var docs = await _collection.Find(filter)
            .SortBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);

        return docs
            .Select(ToDomain)
            .ToList();
    }

    // ---------- Mapping ----------

    private static CampaignParticipation ToDomain(CampaignParticipationDocument doc)
    {
        return CampaignParticipation.FromPersistence(
            id: doc.Id,
            postId: doc.PostId,
            userId: doc.UserId,
            joinedAt: doc.JoinedAt);
    }

    private static CampaignParticipationDocument FromDomain(CampaignParticipation participation)
    {
        return new CampaignParticipationDocument
        {
            Id = participation.Id,
            PostId = participation.PostId,
            UserId = participation.UserId,
            JoinedAt = participation.JoinedAt
        };
    }
}
