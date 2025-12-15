using MongoDB.Bson;
using MongoDB.Driver;
using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Posts;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Persistence.Documents;

namespace Sonic.Infrastructure.Posts;

public sealed class PostRepository : IPostRepository
{
    private readonly IMongoCollection<PostDocument> _collection;

    public PostRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<PostDocument>("posts");
    }

    public async Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var filter = Builders<PostDocument>.Filter.And(
            Builders<PostDocument>.Filter.Eq(x => x.Id, id),
            Builders<PostDocument>.Filter.Eq(x => x.IsDeleted, false));

        var doc = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : ToDomain(doc);
    }

    public async Task InsertAsync(Post post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        var doc = FromDomain(post);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(post);

        var doc = FromDomain(post);
        var filter = Builders<PostDocument>.Filter.Eq(x => x.Id, post.Id);

        await _collection.ReplaceOneAsync(filter, doc, cancellationToken: cancellationToken);
    }

    public async Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var update = Builders<PostDocument>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(
            x => x.Id == id,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<Post>> QueryAsync( // Searcher and Paginator
        int page,
        int pageSize,
        PostType? type = null,
        string? tag = null,
        string? search = null,
        bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var builder = Builders<PostDocument>.Filter;
        var filter = builder.Eq(x => x.IsDeleted, false);

        if (type.HasValue)
        {
            filter &= builder.Eq(x => x.Type, type.Value);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLowerInvariant();
            filter &= builder.AnyEq(x => x.Tags, normalizedTag);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var regex = new BsonRegularExpression(search.Trim(), "i");
            var titleFilter = builder.Regex(x => x.Title, regex);
            var bodyFilter = builder.Regex(x => x.Body, regex);
            filter &= builder.Or(titleFilter, bodyFilter);
        }

        if (featured.HasValue)
        {
            filter &= builder.Eq(x => x.IsFeatured, featured.Value);
        }

        var skip = (page - 1) * pageSize;

        var find = _collection.Find(filter);

        var total = await find.CountDocumentsAsync(cancellationToken);

        var docs = await find
            .SortByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var posts = docs.Select(ToDomain).ToList();

        return new PagedResult<Post>
        {
            Items = posts,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    // ---------- Mapping(Mappers) ----------

    private static Post ToDomain(PostDocument doc)
    {
        return Post.FromPersistence(
            id: doc.Id,
            type: doc.Type,
            title: doc.Title,
            body: doc.Body,
            authorId: doc.AuthorId,
            createdAt: doc.CreatedAt,
            updatedAt: doc.UpdatedAt,
            tags: doc.Tags,
            externalLink: doc.ExternalLink,
            campaignGoal: doc.CampaignGoal,
            isDeleted: doc.IsDeleted,
            isFeatured: doc.IsFeatured);
    }

    private static PostDocument FromDomain(Post post)
    {
        return new PostDocument
        {
            Id = post.Id,
            Type = post.Type,
            Title = post.Title,
            Body = post.Body,
            Tags = post.Tags.ToList(),
            ExternalLink = post.ExternalLink,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsDeleted = post.IsDeleted,
            IsFeatured = post.IsFeatured,
            CampaignGoal = post.CampaignGoal
        };
    }
}
