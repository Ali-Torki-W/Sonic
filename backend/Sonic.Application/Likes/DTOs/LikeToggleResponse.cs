namespace Sonic.Application.Likes.DTOs;

public sealed class LikeToggleResponse
{
    public string PostId { get; set; } = default!;
    public long LikeCount { get; set; }
    public bool Liked { get; set; }
}
