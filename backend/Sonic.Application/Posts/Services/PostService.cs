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
            throw new ArgumentException("AuthorId is required.", nameof(authorId));

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
            throw new ArgumentException("Post id is required.", nameof(id));

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            throw new InvalidOperationException("Post not found.");
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
            throw new ArgumentException("Post id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new ArgumentException("Current user id is required.", nameof(currentUserId));

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            throw new InvalidOperationException("Post not found.");
        }

        if (!isAdmin && !string.Equals(post.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("You are not allowed to update this post.");
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
            throw new ArgumentException("Post id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new ArgumentException("Current user id is required.", nameof(currentUserId));

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post is null)
        {
            throw new InvalidOperationException("Post not found.");
        }

        if (!isAdmin && !string.Equals(post.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this post.");
        }

        // Use domain soft-delete
        post.MarkDeleted();

        await _postRepository.UpdateAsync(post, cancellationToken);
        // SoftDeleteAsync is available for other use-cases (e.g. Admin bulk ops) if needed
    }

    private static PostResponse ToResponse(Post post)
    {
        return new PostResponse
        {
            Id = post.Id,
            Type = post.Type.ToString(),
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
