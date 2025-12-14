using Sonic.Application.Likes.Services;
using Sonic.Application.Posts.Services;
using Sonic.Domain.Posts;
using Sonic.Tests.Fakes;

namespace Sonic.Tests.Tests;

public sealed class LikeServiceTests
{
    [Fact]
    public async Task ToggleLike_AddsThenRemoves()
    {
        var posts = new InMemoryPostRepository();
        var likesRepo = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();

        var postSvc = new PostService(posts, likesRepo, joins);
        var likeSvc = new LikeService(likesRepo, posts);

        var post = await postSvc.CreatePostAsync(new Sonic.Application.Posts.DTOs.CreatePostRequest
        {
            Type = PostType.Experience,
            Title = "T",
            Body = "B"
        }, "owner");

        var r1 = await likeSvc.ToggleLikeAsync(post.Id, userId: "u1");
        Assert.Equal(1, r1.LikeCount);
        Assert.True(r1.Liked);

        var r2 = await likeSvc.ToggleLikeAsync(post.Id, userId: "u1");
        Assert.Equal(0, r2.LikeCount);
        Assert.False(r2.Liked);
    }

    [Fact]
    public async Task LikeCount_TwoUsers_Equals2()
    {
        var posts = new InMemoryPostRepository();
        var likesRepo = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();

        var postSvc = new PostService(posts, likesRepo, joins);
        var likeSvc = new LikeService(likesRepo, posts);

        var post = await postSvc.CreatePostAsync(new Sonic.Application.Posts.DTOs.CreatePostRequest
        {
            Type = PostType.Idea,
            Title = "T",
            Body = "B"
        }, "owner");

        await likeSvc.ToggleLikeAsync(post.Id, "u1");
        var r2 = await likeSvc.ToggleLikeAsync(post.Id, "u2");

        Assert.Equal(2, r2.LikeCount);
    }
}
