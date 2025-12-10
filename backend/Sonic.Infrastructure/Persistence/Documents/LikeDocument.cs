namespace Sonic.Infrastructure.Persistence.Documents;

internal sealed class LikeDocument
{
    public string Id { get; set; } = default!;
    public string PostId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}
