using Sonic.Application.Posts.DTOs;
using Sonic.Application.Posts.Services;
using Sonic.Domain.Posts;
using Sonic.Tests.Fakes;
using Sonic.Tests.TestHelpers;

namespace Sonic.Tests.Tests;

public sealed class PostServiceTests
{
    [Fact]
    public async Task CreatePost_Succeeds()
    {
        var posts = new InMemoryPostRepository();
        var likes = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();

        var svc = new PostService(posts, likes, joins);

        var res = await svc.CreatePostAsync(new CreatePostRequest
        {
            Type = PostType.Experience,
            Title = "Hello",
            Body = "World",
            Tags = new() { "ai", "sonic" }
        }, authorId: "user-1");

        Assert.False(string.IsNullOrWhiteSpace(res.Id));
        Assert.Equal(PostType.Experience, res.Type);
        Assert.Equal("user-1", res.AuthorId);
    }

    [Fact]
    public async Task UpdatePost_AsOwner_Succeeds()
    {
        var posts = new InMemoryPostRepository();
        var likes = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();
        var svc = new PostService(posts, likes, joins);

        var created = await svc.CreatePostAsync(new CreatePostRequest
        {
            Type = PostType.Idea,
            Title = "T1",
            Body = "B1"
        }, authorId: "owner");

        var updated = await svc.UpdatePostAsync(
            id: created.Id,
            request: new UpdatePostRequest
            {
                Title = "T2",
                Body = "B2",
                Tags = new() { "x" }
            },
            currentUserId: "owner",
            isAdmin: false);

        Assert.Equal("T2", updated.Title);
        Assert.Equal("B2", updated.Body);
        Assert.Contains("x", updated.Tags);
    }

    [Fact]
    public async Task UpdatePost_NonOwner_NonAdmin_ReturnsForbidden()
    {
        var posts = new InMemoryPostRepository();
        var likes = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();
        var svc = new PostService(posts, likes, joins);

        var created = await svc.CreatePostAsync(new CreatePostRequest
        {
            Type = PostType.News,
            Title = "T1",
            Body = "B1"
        }, authorId: "owner");

        await ExceptionAssert.AssertStatusCodeAsync(
            () => svc.UpdatePostAsync(
                id: created.Id,
                request: new UpdatePostRequest { Title = "Hack", Body = "Hack" },
                currentUserId: "attacker",
                isAdmin: false),
            expectedStatusCode: 403);
    }

    [Fact]
    public async Task DeletePost_AsOwner_Succeeds()
    {
        var posts = new InMemoryPostRepository();
        var likes = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();
        var svc = new PostService(posts, likes, joins);

        var created = await svc.CreatePostAsync(new CreatePostRequest
        {
            Type = PostType.Course,
            Title = "T1",
            Body = "B1"
        }, authorId: "owner");

        await svc.DeletePostAsync(created.Id, currentUserId: "owner", isAdmin: false);

        // repo GetByIdAsync filters deleted => should be null
        var after = await posts.GetByIdAsync(created.Id);
        Assert.Null(after);
    }
}
