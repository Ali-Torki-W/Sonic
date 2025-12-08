using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Application.Likes.DTOs;
using Sonic.Application.Likes.interfaces;

namespace Sonic.Api.Controllers;

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
}
