using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts;
using Sonic.Application.Posts.DTOs;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Posts;

namespace Sonic.Api.Controllers;

[ApiController]
[Route("posts")]
public class PostController(IPostService postService) : ApiControllerBase
{
    private readonly IPostService _postService = postService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PostResponse>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] PostType? type = null,
        [FromQuery] string? tag = null,
        [FromQuery(Name = "q")] string? search = null,
        [FromQuery] bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetFeedAsync(
            page: page,
            pageSize: pageSize,
            type: type,
            tag: tag,
            search: search,
            featured: featured,
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    // CREATE: POST /posts
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreatePost(
        [FromBody] CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) return BadRequest("Invalid request data.");

        var authorId = User.FindFirst("sub")?.Value;  // Claims-based author ID from JWT

        if (string.IsNullOrEmpty(authorId))
            return Unauthorized("Author is not authenticated.");

        var post = await _postService.CreatePostAsync(request, authorId, cancellationToken);

        var response = new PostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            Tags = post.Tags.ToList(),
            ExternalLink = post.ExternalLink,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsFeatured = post.IsFeatured
        };

        return Ok(response);
    }

    // GET: /posts/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var post = await _postService.GetPostByIdAsync(id, cancellationToken);

        if (post == null)
            return NotFound("Post not found.");

        var response = new PostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            Tags = post.Tags.ToList(),
            ExternalLink = post.ExternalLink,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsFeatured = post.IsFeatured
        };

        return Ok(response);
    }

    // UPDATE: PUT /posts/{id}
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(
        string id,
        [FromBody] UpdatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null) return BadRequest("Invalid request data.");

        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("You must be logged in.");

        var isAdmin = User.IsInRole("Admin");

        var post = await _postService.UpdatePostAsync(id, request, userId, isAdmin, cancellationToken);

        var response = new PostResponse
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            Tags = post.Tags.ToList(),
            ExternalLink = post.ExternalLink,
            AuthorId = post.AuthorId,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            IsFeatured = post.IsFeatured
        };

        return Ok(response);
    }

    // DELETE: DELETE /posts/{id}
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(
        string id,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("You must be logged in.");

        var isAdmin = User.IsInRole("Admin");

        await _postService.DeletePostAsync(id, userId, isAdmin, cancellationToken);

        return NoContent(); // Successfully deleted (soft-delete)
    }
}
