namespace Sonic.Application.Campaigns.DTOs;

public sealed class CampaignJoinResponse
{
    public string PostId { get; set; } = default!;

    public long ParticipantsCount { get; set; }

    /// <summary>
    /// true  => user was added now
    /// false => user had already joined (idempotent)
    /// </summary>
    public bool Joined { get; set; }
}
