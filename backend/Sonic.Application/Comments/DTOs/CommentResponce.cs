namespace Sonic.Application.Comments.DTOs;

public sealed class CommentResponse
{
    public string Id { get; set; } = default!;
    public string PostId { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public string? AuthorDisplayName { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
