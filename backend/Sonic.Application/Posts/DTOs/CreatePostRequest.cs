using Sonic.Domain.Posts;

namespace Sonic.Application.Posts.DTOs;

public sealed class CreatePostRequest
{
    public PostType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    public string? ExternalLink { get; set; }

    public string? CampaignGoal { get; set; }
}
