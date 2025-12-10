using Sonic.Domain.Posts;

namespace Sonic.Infrastructure.Persistence.Documents;

internal sealed class PostDocument
{
    public string Id { get; set; } = default!;
    public PostType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string? ExternalLink { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsFeatured { get; set; }
}
