using Sonic.Application.Common.Errors;
using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts.DTOs;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Posts;

namespace Sonic.Application.Posts.Services;

public sealed class PostService(IPostRepository postRepository) : IPostService
{
    private readonly IPostRepository _postRepository = postRepository;

    public async Task<PostResponse> CreatePostAsync(
        CreatePostRequest request,
        string authorId,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(authorId))
            throw Errors.BadRequest("AuthorId is required.", "post.author_required");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw Errors.BadRequest("Title is required.", "post.title_required");

        if (string.IsNullOrWhiteSpace(request.Body))
            throw Errors.BadRequest("Body is required.", "post.body_required");


        var post = Post.CreateNew(
            type: request.Type,
            title: request.Title,
            body: request.Body,
            authorId: authorId,
            tags: request.Tags,
            externalLink: request.ExternalLink);

        await _postRepository.InsertAsync(post, cancellationToken);

        return ToResponse(post);
    }

    public async Task<PostResponse> GetPostByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw Errors.BadRequest("Post id is required.", "post.id_required");

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            throw Errors.NotFound("Post not found.", "post.not_found");
        }

        return ToResponse(post);
    }

    public async Task<PostResponse> UpdatePostAsync(
        string id,
        UpdatePostRequest request,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(id))
            throw Errors.BadRequest("Post id is required.", "post.id_required");
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw Errors.BadRequest("Current user id is required.", "post.current_user_required");

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            throw Errors.NotFound("Post not found.", "post.not_found");
        }

        if (!isAdmin && !string.Equals(post.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            throw Errors.Forbidden("You are not allowed to update this post.", "post.forbidden_update");
        }

        post.UpdateContent(
            title: request.Title,
            body: request.Body,
            tags: request.Tags,
            externalLink: request.ExternalLink);

        await _postRepository.UpdateAsync(post, cancellationToken);

        return ToResponse(post);
    }

    public async Task DeletePostAsync(
        string id,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw Errors.BadRequest("Post id is required.", "post.id_required");
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw Errors.BadRequest("Current user id is required.", "post.current_user_required");

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            throw Errors.NotFound("Post not found.", "post.not_found");
        }

        if (!isAdmin && !string.Equals(post.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            throw Errors.Forbidden("You are not allowed to delete this post.", "post.forbidden_delete");
        }

        post.MarkDeleted();

        await _postRepository.UpdateAsync(post, cancellationToken);
    }

    public async Task<PagedResult<PostResponse>> GetFeedAsync(
        int page,
        int pageSize,
        PostType? type = null,
        string? tag = null,
        string? search = null,
        bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        // Repository normalizes page/pageSize (page<1 =>1, pageSize<=0=>10),
        // so we don't need to throw 400 here unless you want strict behavior.
        var result = await _postRepository.QueryAsync(
            page: page,
            pageSize: pageSize,
            type: type,
            tag: tag,
            search: search,
            featured: featured,
            cancellationToken: cancellationToken);

        return new PagedResult<PostResponse>
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            Items = result.Items.Select(ToResponse).ToList()
        };
    }


    private static PostResponse ToResponse(Post post)
    {
        return new PostResponse
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
            IsFeatured = post.IsFeatured
        };
    }
}
