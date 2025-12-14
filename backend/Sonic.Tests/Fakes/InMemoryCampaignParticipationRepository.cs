using Sonic.Application.Campaigns.interfaces;
using Sonic.Domain.Campaigns;

namespace Sonic.Tests.Fakes;

public sealed class InMemoryCampaignParticipationRepository : ICampaignParticipationRepository
{
    private readonly HashSet<(string PostId, string UserId)> _joins = new();

    public Task<bool> ExistsAsync(string postId, string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_joins.Contains((postId, userId)));

    public Task AddAsync(CampaignParticipation participation, CancellationToken cancellationToken = default)
    {
        _joins.Add((participation.PostId, participation.UserId));
        return Task.CompletedTask;
    }

    public Task<long> CountForPostAsync(string postId, CancellationToken cancellationToken = default)
        => Task.FromResult(_joins.LongCount(x => x.PostId == postId));

    public Task<IReadOnlyList<CampaignParticipation>> GetByPostIdAsync(string postId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException(); // just for implement our interface
    }
}
