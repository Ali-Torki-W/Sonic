using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Api.Helpers;
using Sonic.Application.Likes.DTOs;
using Sonic.Application.Likes.interfaces;

namespace Sonic.Api.Likes;

[ApiController]
public sealed class LikesController(ILikeService likeService) : ApiControllerBase
{
    private readonly ILikeService _likeService = likeService;

    // POST /posts/{postId}/like
    [HttpPost("posts/{postId}/like")]
    [Authorize]
    public async Task<ActionResult<LikeToggleResponse>> Toggle(
        string postId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var result = await _likeService.ToggleLikeAsync(
            postId,
            currentUserId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("posts/{postId}/like")]
    [Authorize]
    public async Task<ActionResult<LikeToggleResponse>> GetLikStatus(
        string postId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _likeService.GetStatusAsync(postId, userId, cancellationToken);
        return Ok(result);
    }
}
