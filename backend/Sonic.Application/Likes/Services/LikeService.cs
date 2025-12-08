using Sonic.Application.Common.Errors;
using Sonic.Application.Likes.DTOs;
using Sonic.Application.Likes.interfaces;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Likes;

namespace Sonic.Application.Likes.Services;

public sealed class LikeService(
    ILikeRepository likeRepository,
    IPostRepository postRepository) : ILikeService
{
    private readonly ILikeRepository _likeRepository = likeRepository;
    private readonly IPostRepository _postRepository = postRepository;

    public async Task<LikeToggleResponse> ToggleLikeAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
            throw Errors.BadRequest("Post id is required.", "like.post_id_required");

        if (string.IsNullOrWhiteSpace(userId))
            throw Errors.BadRequest("User id is required.", "like.user_id_required");

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post is null)
            throw Errors.NotFound("Post not found.", "post.not_found");

        var exists = await _likeRepository.ExistsAsync(postId, userId, cancellationToken);

        bool nowLiked;

        if (exists)
        {
            await _likeRepository.RemoveAsync(postId, userId, cancellationToken);
            nowLiked = false;
        }
        else
        {
            var like = Like.CreateNew(postId, userId);
            await _likeRepository.AddAsync(like, cancellationToken);
            nowLiked = true;
        }

        var likeCount = await _likeRepository.CountForPostAsync(postId, cancellationToken);

        return new LikeToggleResponse
        {
            PostId = postId,
            LikeCount = likeCount,
            Liked = nowLiked
        };
    }
}
