using Sonic.Application.Comments.DTOs;
using Sonic.Application.Comments.Services;
using Sonic.Application.Posts.Services;
using Sonic.Domain.Posts;
using Sonic.Tests.Fakes;
using Sonic.Tests.TestHelpers;

namespace Sonic.Tests.Tests;

public sealed class CommentServiceTests
{
    [Fact]
    public async Task AddComment_Succeeds()
    {
        var posts = new InMemoryPostRepository();
        var users = new InMemoryUserRepository();
        var likes = new InMemoryLikeRepository();
        var joins = new InMemoryCampaignParticipationRepository();
        var postSvc = new PostService(posts, likes, joins);

        var commentsRepo = new InMemoryCommentRepository();
        var commentSvc = new CommentService(commentsRepo, posts, users);

        var post = await postSvc.CreatePostAsync(new Sonic.Application.Posts.DTOs.CreatePostRequest
        {
            Type = PostType.Experience,
            Title = "T",
            Body = "B"
        }, authorId: "u1");

        var res = await commentSvc.AddCommentAsync(
            postId: post.Id,
            request: new CreateCommentRequest { Body = "Nice" },
            authorId: "u2");

        Assert.False(string.IsNullOrWhiteSpace(res.Id));
        Assert.Equal(post.Id, res.PostId);
        Assert.Equal("u2", res.AuthorId);
    }

    [Fact]
    public async Task DeleteComment_AsAuthor_Succeeds()
    {
        var posts = new InMemoryPostRepository();
        var users = new InMemoryUserRepository();
        var commentsRepo = new InMemoryCommentRepository();
        var commentSvc = new CommentService(commentsRepo, posts, users);

        // Insert a post directly for existence check
        var p = Sonic.Domain.Posts.Post.CreateNew(PostType.Idea, "T", "B", "owner");
        await posts.InsertAsync(p);

        var c = await commentSvc.AddCommentAsync(
            p.Id,
            authorId: "author",
            new CreateCommentRequest { Body = "C1" }
            );

        await commentSvc.DeleteCommentAsync(c.Id, currentUserId: "author", isAdmin: false);

        var after = await commentsRepo.GetByIdAsync(c.Id);
        Assert.Null(after);
    }

    [Fact]
    public async Task DeleteComment_NonAuthor_NonAdmin_ReturnsForbidden()
    {
        var posts = new InMemoryPostRepository();
        var users = new InMemoryUserRepository();
        var commentsRepo = new InMemoryCommentRepository();
        var commentSvc = new CommentService(commentsRepo, posts, users);

        var p = Sonic.Domain.Posts.Post.CreateNew(PostType.Idea, "T", "B", "owner");
        await posts.InsertAsync(p);

        var c = await commentSvc.AddCommentAsync(
            p.Id,
            authorId: "author",
            new CreateCommentRequest { Body = "C1" }
            );

        await ExceptionAssert.AssertStatusCodeAsync(
            () => commentSvc.DeleteCommentAsync(c.Id, currentUserId: "attacker", isAdmin: false),
            expectedStatusCode: 403);
    }
}
