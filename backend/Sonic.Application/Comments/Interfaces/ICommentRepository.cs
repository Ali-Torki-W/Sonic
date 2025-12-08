using Sonic.Application.Common.Pagination;
using Sonic.Domain.Comments;

namespace Sonic.Application.Comments.interfaces;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task InsertAsync(
        Comment comment,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Comment>> GetByPostIdAsync(
        string postId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(
        string id,
        CancellationToken cancellationToken = default);
}
