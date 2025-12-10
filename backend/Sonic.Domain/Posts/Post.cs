using System;
using System.Collections.Generic;
using System.Linq;

namespace Sonic.Domain.Posts;

public sealed class Post
{
    private readonly List<string> _tags = new();

    public string Id { get; }
    public PostType Type { get; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();
    public string? ExternalLink { get; private set; }
    public string AuthorId { get; }

    // NEW: only meaningful when Type == Campaign
    public string? CampaignGoal { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }
    public bool IsFeatured { get; private set; }

    // Main private ctor – only this sets fields directly
    private Post(
        string id,
        PostType type,
        string title,
        string body,
        string authorId,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<string>? tags,
        string? externalLink,
        string? campaignGoal,
        bool isDeleted,
        bool isFeatured)
    {
        Id = id;
        Type = type;
        Title = title;
        Body = body;
        AuthorId = authorId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        IsDeleted = isDeleted;
        IsFeatured = isFeatured;
        ExternalLink = externalLink;

        CampaignGoal = string.IsNullOrWhiteSpace(campaignGoal)
            ? null
            : campaignGoal.Trim();

        if (tags is not null)
        {
            _tags.AddRange(NormalizeTags(tags));
        }

        Validate();
    }

    // Factory for new posts (used by Application when creating)
    public static Post CreateNew(
        PostType type,
        string title,
        string body,
        string authorId,
        IEnumerable<string>? tags = null,
        string? externalLink = null,
        string? campaignGoal = null)
    {
        if (string.IsNullOrWhiteSpace(authorId))
        {
            throw new ArgumentException("AuthorId is required.", nameof(authorId));
        }

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid().ToString("N");

        // Only allow goal for Campaign posts; ignore it for others
        var normalizedGoal = type == PostType.Campaign
            ? (string.IsNullOrWhiteSpace(campaignGoal) ? null : campaignGoal.Trim())
            : null;

        return new Post(
            id: id,
            type: type,
            title: title,
            body: body,
            authorId: authorId.Trim(),
            createdAt: now,
            updatedAt: now,
            tags: tags,
            externalLink: externalLink,
            campaignGoal: normalizedGoal,
            isDeleted: false,
            isFeatured: false);
    }

    // Factory for rehydrating from Mongo (Infra will call this)
    public static Post FromPersistence(
        string id,
        PostType type,
        string title,
        string body,
        string authorId,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<string>? tags,
        string? externalLink,
        string? campaignGoal,
        bool isDeleted,
        bool isFeatured)
    {
        return new Post(
            id: id,
            type: type,
            title: title,
            body: body,
            authorId: authorId,
            createdAt: createdAt,
            updatedAt: updatedAt,
            tags: tags,
            externalLink: externalLink,
            campaignGoal: campaignGoal,
            isDeleted: isDeleted,
            isFeatured: isFeatured);
    }

    public void UpdateContent(
        string title,
        string body,
        IEnumerable<string>? tags,
        string? externalLink,
        string? campaignGoal = null)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a deleted post.");
        }

        Title = title;
        Body = body;

        _tags.Clear();
        if (tags is not null)
        {
            _tags.AddRange(NormalizeTags(tags));
        }

        ExternalLink = string.IsNullOrWhiteSpace(externalLink)
            ? null
            : externalLink.Trim();

        CampaignGoal = Type == PostType.Campaign
            ? (string.IsNullOrWhiteSpace(campaignGoal) ? null : campaignGoal.Trim())
            : null;

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

    public void SetFeatured(bool isFeatured)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Cannot change featured state of a deleted post.");
        }

        IsFeatured = isFeatured;
        UpdatedAt = DateTime.UtcNow;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("Post title is required.");
        }

        Title = Title.Trim();

        if (string.IsNullOrWhiteSpace(Body))
        {
            throw new InvalidOperationException("Post body is required.");
        }

        Body = Body.Trim();

        if (string.IsNullOrWhiteSpace(AuthorId))
        {
            throw new InvalidOperationException("AuthorId is required.");
        }

        // Normalize external link
        if (!string.IsNullOrWhiteSpace(ExternalLink))
        {
            ExternalLink = ExternalLink.Trim();
        }

        // If not a Campaign, do not keep any goal
        if (Type != PostType.Campaign)
        {
            CampaignGoal = null;
        }

        // You *could* add extra constraints for campaign goal later (length, etc.)
    }

    private static IEnumerable<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct();
    }
}
