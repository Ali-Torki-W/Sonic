using Sonic.Application.Common.Pagination;
using Sonic.Domain.Posts;

namespace Sonic.Application.Posts.interfaces;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task InsertAsync(Post post, CancellationToken cancellationToken = default);

    Task UpdateAsync(Post post, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<PagedResult<Post>> QueryAsync(
        int page,
        int pageSize,
        PostType? type = null,
        string? tag = null,
        string? search = null,
        bool? featured = null,
        CancellationToken cancellationToken = default);
}
