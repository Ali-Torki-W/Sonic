using Sonic.Application.Campaigns.interfaces;
using Sonic.Application.Common.Errors;
using Sonic.Application.Common.Pagination;
using Sonic.Application.Likes.interfaces;
using Sonic.Application.Posts.DTOs;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Posts;

namespace Sonic.Application.Posts.Services;

public sealed class PostService(
    IPostRepository postRepository,
    ILikeRepository likeRepository,
    ICampaignParticipationRepository campaignParticipationRepository) : IPostService
{
    private readonly IPostRepository _postRepository = postRepository;
    private readonly ILikeRepository _likeRepository = likeRepository;
    private readonly ICampaignParticipationRepository _campaignParticipationRepository = campaignParticipationRepository;

    public async Task<PostResponse> CreatePostAsync(
        CreatePostRequest request,
        string authorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(authorId))
        {
            throw Errors.BadRequest("Author id is required.", "post.author_id_required");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw Errors.BadRequest("Title is required.", "post.title_required");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw Errors.BadRequest("Body is required.", "post.body_required");
        }

        var post = Domain.Posts.Post.CreateNew(
            type: request.Type,
            title: request.Title,
            body: request.Body,
            authorId: authorId,
            tags: request.Tags,
            externalLink: request.ExternalLink,
            campaignGoal: request.CampaignGoal);

        await _postRepository.InsertAsync(post, cancellationToken);

        // New post: likes and participants start at 0
        return ToResponse(post, likeCount: 0, participantsCount: 0);
    }

    public async Task<PostResponse> GetPostByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Errors.BadRequest("Post id is required.", "post.id_required");
        }

        var post = await _postRepository.GetByIdAsync(id, cancellationToken) ?? throw Errors.NotFound("Post not found.", "post.not_found");
        var likeCount = await _likeRepository.CountForPostAsync(id, cancellationToken);

        long participantsCount = 0;
        if (post.Type == PostType.Campaign)
        {
            participantsCount = await _campaignParticipationRepository.CountForPostAsync(id, cancellationToken);
        }

        return ToResponse(post, likeCount, participantsCount);
    }

    public async Task<PostResponse> UpdatePostAsync(
        string id,
        UpdatePostRequest request,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(id))
        {
            throw Errors.BadRequest("Post id is required.", "post.id_required");
        }

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw Errors.BadRequest("Current user id is required.", "post.current_user_required");
        }

        var post = await _postRepository.GetByIdAsync(id, cancellationToken) ?? throw Errors.NotFound("Post not found.", "post.not_found");
        if (!isAdmin && !string.Equals(post.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            throw Errors.Forbidden("You are not allowed to update this post.", "post.forbidden_update");
        }

        post.UpdateContent(
            title: request.Title,
            body: request.Body,
            tags: request.Tags,
            externalLink: request.ExternalLink,
            campaignGoal: request.CampaignGoal);

        await _postRepository.UpdateAsync(post, cancellationToken);

        var likeCount = await _likeRepository.CountForPostAsync(id, cancellationToken);

        long participantsCount = 0;
        if (post.Type == PostType.Campaign)
        {
            participantsCount = await _campaignParticipationRepository.CountForPostAsync(id, cancellationToken);
        }

        return ToResponse(post, likeCount, participantsCount);
    }

    public async Task DeletePostAsync(
        string id,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Errors.BadRequest("Post id is required.", "post.id_required");
        }

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw Errors.BadRequest("Current user id is required.", "post.current_user_required");
        }

        var post = await _postRepository.GetByIdAsync(id, cancellationToken) ?? throw Errors.NotFound("Post not found.", "post.not_found");
        if (!isAdmin && !string.Equals(post.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            throw Errors.Forbidden("You are not allowed to delete this post.", "post.forbidden_delete");
        }

        post.MarkDeleted();
        await _postRepository.UpdateAsync(post, cancellationToken);
        // We keep likes and participations; post is no longer visible in normal queries.
    }

    public async Task<PagedResult<PostResponse>> GetFeedAsync(
        int page,
        int pageSize,
        PostType? type = null,
        List<string>? tag = null,
        string? search = null,
        bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _postRepository.QueryAsync(
            page: page,
            pageSize: pageSize,
            type: type,
            tag: tag,
            search: search,
            featured: featured,
            cancellationToken: cancellationToken);

        if (result.TotalItems == 0)
        {
            return new PagedResult<PostResponse>
            {
                Items = Array.Empty<PostResponse>(),
                Page = result.Page,
                PageSize = result.PageSize,
                TotalItems = 0
            };
        }

        var responses = new List<PostResponse>(result.Items.Count);

        foreach (var post in result.Items)
        {
            var likeCount = await _likeRepository.CountForPostAsync(post.Id, cancellationToken);

            long participantsCount = 0;
            if (post.Type == PostType.Campaign)
            {
                participantsCount = await _campaignParticipationRepository.CountForPostAsync(post.Id, cancellationToken);
            }

            responses.Add(ToResponse(post, likeCount, participantsCount));
        }

        return new PagedResult<PostResponse>
        {
            Items = responses,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems
        };
    }

    public async Task SetFeaturedStatusAsync(
        string id,
        bool isFeatured,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw Errors.BadRequest("Post id is required.", "post.id_required");
        }

        var post = await _postRepository.GetByIdAsync(id, cancellationToken) ?? throw Errors.NotFound("Post not found.", "post.not_found");

        // Domain rule: can't feature deleted posts (Post.SetFeatured will also guard)
        post.SetFeatured(isFeatured);

        await _postRepository.UpdateAsync(post, cancellationToken);
    }

    private static PostResponse ToResponse(
        Domain.Posts.Post post,
        long likeCount,
        long participantsCount)
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
            IsFeatured = post.IsFeatured,
            LikeCount = likeCount,
            CampaignGoal = post.CampaignGoal,
            ParticipantsCount = participantsCount
        };
    }
}
