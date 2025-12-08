using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Application.Comments.DTOs;
using Sonic.Application.Comments.interfaces;
using Sonic.Application.Common.Pagination;

namespace Sonic.Api.Controllers;

[ApiController]
public sealed class CommentsController(ICommentService commentService) : ApiControllerBase
{
    private readonly ICommentService _commentService = commentService;

    // POST /posts/{postId}/comments
    [HttpPost("posts/{postId}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentResponse>> Add(
        string postId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var result = await _commentService.AddCommentAsync(
            postId, currentUserId, request, cancellationToken);

        return CreatedAtAction(
            nameof(GetForPost),
            new { postId, page = 1, pageSize = 20 },
            result);
    }

    // GET /posts/{postId}/comments
    [HttpGet("posts/{postId}/comments")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<CommentResponse>>> GetForPost(
        string postId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _commentService.GetCommentsForPostAsync(
            postId, page, pageSize, cancellationToken);

        return Ok(result);
    }

    // DELETE /comments/{id}
    [HttpDelete("comments/{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();

        await _commentService.DeleteCommentAsync(
            id, currentUserId, isAdmin, cancellationToken);

        return NoContent();
    }
}
