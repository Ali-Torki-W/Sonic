using MongoDB.Driver;
using Sonic.Application.Comments.interfaces;
using Sonic.Application.Common.Pagination;
using Sonic.Domain.Comments;
using Sonic.Infrastructure.Config;
using Sonic.Infrastructure.Persistence.Documents;

namespace Sonic.Infrastructure.Comments;

public sealed class CommentRepository : ICommentRepository
{
    private readonly IMongoCollection<CommentDocument> _collection;

    public CommentRepository(MongoDbContext dbContext)
    {
        _collection = dbContext.GetCollection<CommentDocument>("comments");
    }

    public async Task<Comment?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var filter = Builders<CommentDocument>.Filter.And(
            Builders<CommentDocument>.Filter.Eq(x => x.Id, id),
            Builders<CommentDocument>.Filter.Eq(x => x.IsDeleted, false));

        var doc = await _collection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);

        return doc is null ? null : ToDomain(doc);
    }

    public async Task InsertAsync(
        Comment comment,
        CancellationToken cancellationToken = default)
    {
        if (comment is null) throw new ArgumentNullException(nameof(comment));

        var doc = FromDomain(comment);
        await _collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<Comment>> GetByPostIdAsync(
        string postId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            return new PagedResult<Comment>
            {
                Items = Array.Empty<Comment>(), // no content
                Page = 1,
                PageSize = 0,
                TotalItems = 0
            };
        }

        if (page < 1)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var builder = Builders<CommentDocument>.Filter;
        var filter = builder.And(
            builder.Eq(x => x.PostId, postId),
            builder.Eq(x => x.IsDeleted, false));

        var skip = (page - 1) * pageSize;

        var find = _collection.Find(filter);

        var total = await find.CountDocumentsAsync(cancellationToken);

        // Chronological: oldest first
        var docs = await find
            .SortBy(x => x.CreatedAt)
            .Skip(skip)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var comments = docs.Select(ToDomain).ToList();

        return new PagedResult<Comment>
        {
            Items = comments,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task SoftDeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var update = Builders<CommentDocument>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateOneAsync(
            x => x.Id == id,
            update,
            cancellationToken: cancellationToken);
    }

    // ---------- Mapping ----------

    private static Comment ToDomain(CommentDocument doc)
    {
        return Comment.FromPersistence(
            id: doc.Id,
            postId: doc.PostId,
            authorId: doc.AuthorId,
            body: doc.Body,
            createdAt: doc.CreatedAt,
            updatedAt: doc.UpdatedAt,
            isDeleted: doc.IsDeleted);
    }

    private static CommentDocument FromDomain(Comment comment)
    {
        return new CommentDocument
        {
            Id = comment.Id,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsDeleted = comment.IsDeleted
        };
    }
}
