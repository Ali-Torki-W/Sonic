using System;

namespace Sonic.Infrastructure.Persistence.Documents;

internal sealed class CommentDocument
{
    public string Id { get; set; } = default!;
    public string PostId { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
