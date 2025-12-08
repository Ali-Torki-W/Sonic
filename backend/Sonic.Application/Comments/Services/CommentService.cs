using Sonic.Application.Comments.DTOs;
using Sonic.Application.Comments.interfaces;
using Sonic.Application.Common.Errors;
using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts.interfaces;
using Sonic.Application.Users;
using Sonic.Domain.Comments;

namespace Sonic.Application.Comments.Services;

public sealed class CommentService(
    ICommentRepository commentRepository,
    IPostRepository postRepository,
    IUserRepository userRepository) : ICommentService
{
    private readonly ICommentRepository _commentRepository = commentRepository;
    private readonly IPostRepository _postRepository = postRepository;
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<CommentResponse> AddCommentAsync(
        string postId,
        string authorId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(postId))
            throw Errors.BadRequest("Post id is required.", "comment.post_id_required");

        if (string.IsNullOrWhiteSpace(authorId))
            throw Errors.BadRequest("Author id is required.", "comment.author_id_required");

        if (string.IsNullOrWhiteSpace(request.Body))
            throw Errors.BadRequest("Comment body is required.", "comment.body_required");

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post is null)
            throw Errors.NotFound("Post not found.", "post.not_found");

        var comment = Comment.CreateNew(postId, authorId, request.Body);

        await _commentRepository.InsertAsync(comment, cancellationToken);

        var author = await _userRepository.GetByIdAsync(authorId, cancellationToken);
        var displayName = author?.DisplayName;

        return ToResponse(comment, displayName);
    }

    public async Task<PagedResult<CommentResponse>> GetCommentsForPostAsync(
        string postId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
            throw Errors.BadRequest("Post id is required.", "comment.post_id_required");

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post is null)
            throw Errors.NotFound("Post not found.", "post.not_found");

        var result = await _commentRepository.GetByPostIdAsync(
            postId,
            page,
            pageSize,
            cancellationToken);

        if (result.TotalItems == 0)
        {
            return new PagedResult<CommentResponse>
            {
                Items = Array.Empty<CommentResponse>(),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = 0
            };
        }

        var authorIds = result.Items
            .Select(c => c.AuthorId)
            .Distinct()
            .ToList();

        var authorLookup = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var authorId in authorIds)
        {
            var user = await _userRepository.GetByIdAsync(authorId, cancellationToken);
            authorLookup[authorId] = user?.DisplayName;
        }

        var responses = result.Items
            .Select(c =>
            {
                authorLookup.TryGetValue(c.AuthorId, out var name);
                return ToResponse(c, name);
            })
            .ToList();

        return new PagedResult<CommentResponse>
        {
            Items = responses,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems
        };
    }

    public async Task DeleteCommentAsync(
        string commentId,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commentId))
            throw Errors.BadRequest("Comment id is required.", "comment.id_required");

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw Errors.BadRequest("Current user id is required.", "comment.current_user_required");

        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment is null)
            throw Errors.NotFound("Comment not found.", "comment.not_found");

        if (!isAdmin && !string.Equals(comment.AuthorId, currentUserId, StringComparison.Ordinal))
            throw Errors.Forbidden("You are not allowed to delete this comment.", "comment.forbidden_delete");

        await _commentRepository.SoftDeleteAsync(commentId, cancellationToken);
    }

    private static CommentResponse ToResponse(Comment comment, string? authorDisplayName)
    {
        return new CommentResponse
        {
            Id = comment.Id,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            AuthorDisplayName = authorDisplayName,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
