using Sonic.Application.Common.Pagination;
using Sonic.Application.Posts.DTOs;
using Sonic.Domain.Posts;

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

    Task<PagedResult<PostResponse>> GetFeedAsync(
        int page,
        int pageSize,
        PostType? type = null,
        List<string>? tag = null,
        string? search = null,
        bool? featured = null,
        CancellationToken cancellationToken = default);

    Task SetFeaturedStatusAsync(
       string id,
       bool isFeatured,
       CancellationToken cancellationToken = default);
}
