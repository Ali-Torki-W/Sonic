using Sonic.Application.Posts.DTOs;

namespace Sonic.Application.Posts.interfaces;

public interface IPostService
{
    Task<PostResponse> CreatePostAsync(
        CreatePostRequest request,
        string authorId,
        CancellationToken cancellationToken = default);

    Task<PostResponse> GetPostByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<PostResponse> UpdatePostAsync(
        string id,
        UpdatePostRequest request,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task DeletePostAsync(
        string id,
        string currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
