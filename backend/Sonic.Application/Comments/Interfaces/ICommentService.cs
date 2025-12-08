using Sonic.Application.Comments.DTOs;
using Sonic.Application.Common.Pagination;

namespace Sonic.Application.Comments.interfaces;

public interface ICommentService
{
    Task<CommentResponse> AddCommentAsync(
        string postId,
        string authorId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<CommentResponse>> GetCommentsForPostAsync(
        string postId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task DeleteCommentAsync(
        string commentId,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
