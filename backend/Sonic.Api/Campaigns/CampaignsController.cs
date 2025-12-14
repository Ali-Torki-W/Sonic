using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Api.Helpers;
using Sonic.Application.Campaigns.DTOs;
using Sonic.Application.Campaigns.interfaces;
using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts.DTOs;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Posts;

namespace Sonic.Api.Campaigns;

[ApiController]
public sealed class CampaignsController(
    ICampaignService campaignService,
    IPostService postService) : ApiControllerBase
{
    private readonly ICampaignService _campaignService = campaignService;
    private readonly IPostService _postService = postService;

    // POST /campaigns/{postId}/join
    [HttpPost("campaigns/{postId}/join")]
    [Authorize]
    public async Task<ActionResult<CampaignJoinResponse>> Join(
        string postId,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        var result = await _campaignService.JoinCampaignAsync(
            postId,
            currentUserId,
            cancellationToken);

        return Ok(result);
    }

    // GET /campaigns
    // Thin wrapper over /posts?type=Campaign
    [HttpGet("campaigns")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<PostResponse>>> GetCampaigns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? tag = null,
        [FromQuery(Name = "q")] string? search = null,
        [FromQuery] bool? featured = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetFeedAsync(
            page: page,
            pageSize: pageSize,
            type: PostType.Campaign,
            tag: tag,
            search: search,
            featured: featured,
            cancellationToken: cancellationToken);

        return Ok(result);
    }
}
