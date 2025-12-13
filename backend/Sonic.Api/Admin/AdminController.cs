using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Api.Helpers;
using Sonic.Application.Comments.interfaces;
using Sonic.Application.Posts.interfaces;

namespace Sonic.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminController(
    IPostService postService,
    ICommentService commentService) : ApiControllerBase
{
    private readonly IPostService _postService = postService;
    private readonly ICommentService _commentService = commentService;

    // DELETE /admin/posts/{id}
    [HttpDelete("posts/{id}")]
    public async Task<IActionResult> DeletePost(
        string id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        await _postService.DeletePostAsync(
            id: id,
            currentUserId: currentUserId,
            isAdmin: true,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    // DELETE /admin/comments/{id}
    [HttpDelete("comments/{id}")]
    public async Task<IActionResult> DeleteComment(
        string id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        await _commentService.DeleteCommentAsync(
            commentId: id,
            currentUserId: currentUserId,
            isAdmin: true,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    // NEW: POST /admin/posts/{id}/feature
    [HttpPost("posts/{id}/feature")]
    public async Task<IActionResult> FeaturePost(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _postService.SetFeaturedStatusAsync(
            id: id,
            isFeatured: true,
            cancellationToken: cancellationToken);

        return NoContent();
    }

    // NEW (optional but useful): POST /admin/posts/{id}/unfeature
    [HttpPost("posts/{id}/unfeature")]
    public async Task<IActionResult> UnfeaturePost(
        string id,
        CancellationToken cancellationToken = default)
    {
        await _postService.SetFeaturedStatusAsync(
            id: id,
            isFeatured: false,
            cancellationToken: cancellationToken);

        return NoContent();
    }
}
