using Sonic.Application.Common.Pagination;
using Sonic.Application.Comments.interfaces;
using Sonic.Domain.Comments;

namespace Sonic.Tests.Fakes;

public sealed class InMemoryCommentRepository : ICommentRepository
{
    private readonly Dictionary<string, Comment> _byId = new(StringComparer.Ordinal);

    public Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _byId[comment.Id] = comment;
        return Task.CompletedTask;
    }

    public Task<Comment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.FromResult<Comment?>(null);
        }

        if (_byId.TryGetValue(id, out var c) && !c.IsDeleted)
        {
            return Task.FromResult<Comment?>(c);
        }

        return Task.FromResult<Comment?>(null);
    }

    public Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_byId.TryGetValue(id, out var c))
        {
            c.MarkDeleted();
            _byId[id] = c;
        }

        return Task.CompletedTask;
    }

    public Task<PagedResult<Comment>> GetByPostIdAsync(
        string postId,
        int page,
        int pageSize,
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

        var q = _byId.Values
            .Where(x => !x.IsDeleted && string.Equals(x.PostId, postId, StringComparison.Ordinal))
            .OrderBy(x => x.CreatedAt);

        var total = q.LongCount();
        var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<Comment>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        });
    }

    public Task InsertAsync(Comment comment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comment);

        _byId[comment.Id] = comment;
        return Task.CompletedTask;
    }

}
