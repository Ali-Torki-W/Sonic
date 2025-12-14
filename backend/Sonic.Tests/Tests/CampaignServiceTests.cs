using Sonic.Application.Campaigns.Services;
using Sonic.Domain.Posts;
using Sonic.Tests.Fakes;

namespace Sonic.Tests.Tests;

public sealed class CampaignServiceTests
{
    [Fact]
    public async Task JoinCampaign_FirstJoin_CreatesParticipation()
    {
        var posts = new InMemoryPostRepository();
        var joins = new InMemoryCampaignParticipationRepository();

        // create a campaign post
        var campaign = Sonic.Domain.Posts.Post.CreateNew(
            type: PostType.Campaign,
            title: "C",
            body: "B",
            authorId: "owner",
            tags: null,
            externalLink: null,
            campaignGoal: "Goal");
        await posts.InsertAsync(campaign);

        var svc = new CampaignService(joins, posts);

        var r1 = await svc.JoinCampaignAsync(campaign.Id, "u1");
        Assert.True(r1.Joined);
        Assert.Equal(1, r1.ParticipantsCount);

        var r2 = await svc.JoinCampaignAsync(campaign.Id, "u1");
        Assert.False(r2.Joined);
        Assert.Equal(1, r2.ParticipantsCount);
    }
}
