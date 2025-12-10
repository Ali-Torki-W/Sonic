using Sonic.Application.Campaigns.DTOs;

namespace Sonic.Application.Campaigns.interfaces;

public interface ICampaignService
{
    Task<CampaignJoinResponse> JoinCampaignAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default);
}
