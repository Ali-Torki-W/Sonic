using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sonic.Api.Helpers;
using Sonic.Application.Users.DTOs;
using Sonic.Application.Users.interfaces;

namespace Sonic.Api.Users;

[ApiController]
[Route("users")]
public sealed class UsersController(IUserService userService) : ApiControllerBase
{
    private readonly IUserService _userService = userService;

    // GET /users/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<GetCurrentUserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(result);
    }

    // PUT /users/me
    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<GetCurrentUserResponse>> UpdateMe(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(result);
    }

    // Optional MVP Lite: GET /users/{id}
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileResponse>> GetPublicProfile(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetPublicProfileAsync(id, cancellationToken);
        return Ok(result);
    }
}
