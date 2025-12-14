using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Posts;

namespace Sonic.Tests.Fakes;

public sealed class InMemoryPostRepository : IPostRepository
{
    private readonly Dictionary<string, Post> _posts = new(StringComparer.Ordinal);

    public Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<Post?>(null);

        if (_posts.TryGetValue(id, out var post) && !post.IsDeleted)
            return Task.FromResult<Post?>(post);

        return Task.FromResult<Post?>(null);
    }

    public Task InsertAsync(Post post, CancellationToken cancellationToken = default)
    {
        _posts[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        _posts[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_posts.TryGetValue(id, out var post))
        {
            post.MarkDeleted();
            _posts[id] = post;
        }
        return Task.CompletedTask;
    }

    public Task<PagedResult<Post>> QueryAsync(
        int page,
        int pageSize,
        PostType? type = null,
        string? tag = null,
        string? search = null,
        bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 10;

        IEnumerable<Post> q = _posts.Values.Where(p => !p.IsDeleted);

        if (type.HasValue) q = q.Where(p => p.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var t = tag.Trim().ToLowerInvariant();
            q = q.Where(p => p.Tags.Any(x => string.Equals(x, t, StringComparison.Ordinal)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p =>
                p.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                p.Body.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (featured.HasValue) q = q.Where(p => p.IsFeatured == featured.Value);

        q = q.OrderByDescending(p => p.CreatedAt);

        var total = q.LongCount();
        var items = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PagedResult<Post>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        });
    }
}
