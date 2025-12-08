using System;

namespace Sonic.Domain.Comments;

public sealed class Comment
{
    public string Id { get; }
    public string PostId { get; }
    public string AuthorId { get; }
    public string Body { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    private Comment(
        string id,
        string postId,
        string authorId,
        string body,
        DateTime createdAt,
        DateTime? updatedAt,
        bool isDeleted)
    {
        Id = id;
        PostId = postId;
        AuthorId = authorId;
        Body = body;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        IsDeleted = isDeleted;

        Validate();
    }

    public static Comment CreateNew(
        string postId,
        string authorId,
        string body)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw new ArgumentException("PostId is required.", nameof(postId));
        }

        if (string.IsNullOrWhiteSpace(authorId))
        {
            throw new ArgumentException("AuthorId is required.", nameof(authorId));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid().ToString("N");

        return new Comment(
            id: id,
            postId: postId.Trim(),
            authorId: authorId.Trim(),
            body: body,
            createdAt: now,
            updatedAt: null,
            isDeleted: false);
    }

    public static Comment FromPersistence(
        string id,
        string postId,
        string authorId,
        string body,
        DateTime createdAt,
        DateTime? updatedAt,
        bool isDeleted)
    {
        return new Comment(
            id: id,
            postId: postId,
            authorId: authorId,
            body: body,
            createdAt: createdAt,
            updatedAt: updatedAt,
            isDeleted: isDeleted);
    }

    public void UpdateBody(string newBody)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Cannot edit a deleted comment.");
        }

        if (string.IsNullOrWhiteSpace(newBody))
        {
            throw new InvalidOperationException("Comment body is required.");
        }

        Body = newBody.Trim();
        UpdatedAt = DateTime.UtcNow;

        Validate();
    }

    public void MarkDeleted()
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(PostId))
        {
            throw new InvalidOperationException("PostId is required.");
        }

        if (string.IsNullOrWhiteSpace(AuthorId))
        {
            throw new InvalidOperationException("AuthorId is required.");
        }

        if (string.IsNullOrWhiteSpace(Body))
        {
            throw new InvalidOperationException("Comment body is required.");
        }

        Body = Body.Trim();
    }
}
