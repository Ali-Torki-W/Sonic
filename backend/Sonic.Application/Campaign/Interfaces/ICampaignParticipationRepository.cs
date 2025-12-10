using Sonic.Domain.Campaigns;

namespace Sonic.Application.Campaigns.interfaces;

public interface ICampaignParticipationRepository
{
    Task<bool> ExistsAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CampaignParticipation participation,
        CancellationToken cancellationToken = default);

    Task<long> CountForPostAsync(
        string postId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CampaignParticipation>> GetByPostIdAsync(
        string postId,
        CancellationToken cancellationToken = default);
}
