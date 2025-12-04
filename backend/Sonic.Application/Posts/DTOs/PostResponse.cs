namespace Sonic.Application.Posts.DTOs;

public sealed class PostResponse
{
    public string Id { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public string? ExternalLink { get; init; }

    public string AuthorId { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public bool IsFeatured { get; init; }
}
