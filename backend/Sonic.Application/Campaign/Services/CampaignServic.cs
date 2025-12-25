using Sonic.Application.Campaigns.DTOs;
using Sonic.Application.Campaigns.interfaces;
using Sonic.Application.Common.Errors;
using Sonic.Application.Posts.interfaces;
using Sonic.Domain.Campaigns;
using Sonic.Domain.Posts;

namespace Sonic.Application.Campaigns.Services;

public sealed class CampaignService(
    ICampaignParticipationRepository participationRepository,
    IPostRepository postRepository) : ICampaignService
{
    private readonly ICampaignParticipationRepository _participationRepository = participationRepository;
    private readonly IPostRepository _postRepository = postRepository;

    public async Task<CampaignJoinResponse> JoinCampaignAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
        {
            throw Errors.BadRequest("Campaign post id is required.", "campaign.post_id_required");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw Errors.BadRequest("User id is required.", "campaign.user_id_required");
        }

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken)
                   ?? throw Errors.NotFound("Campaign not found.", "campaign.not_found");

        if (post.Type != PostType.Campaign)
        {
            throw Errors.BadRequest("Target post is not a campaign.", "campaign.invalid_type");
        }

        var alreadyExists = await _participationRepository.ExistsAsync(postId, userId, cancellationToken);

        var joinedNow = false;

        if (!alreadyExists)
        {
            var participation = CampaignParticipation.CreateNew(postId, userId);
            await _participationRepository.AddAsync(participation, cancellationToken);
            joinedNow = true;
        }

        var count = await _participationRepository.CountForPostAsync(postId, cancellationToken);

        return new CampaignJoinResponse
        {
            PostId = postId,
            ParticipantsCount = count,
            Joined = alreadyExists || joinedNow
        };
    }

    public async Task<CampaignJoinResponse> GetJoinStatusAsync(
        string postId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(postId))
            throw Errors.BadRequest("Campaign post id is required.", "campaign.post_id_required");

        if (string.IsNullOrWhiteSpace(userId))
            throw Errors.BadRequest("User id is required.", "campaign.user_id_required");

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken)
                   ?? throw Errors.NotFound("Campaign not found.", "campaign.not_found");

        if (post.Type != PostType.Campaign)
            throw Errors.BadRequest("Target post is not a campaign.", "campaign.invalid_type");

        var alreadyExists = await _participationRepository.ExistsAsync(postId, userId, cancellationToken);
        var count = await _participationRepository.CountForPostAsync(postId, cancellationToken);

        return new CampaignJoinResponse
        {
            PostId = postId,
            ParticipantsCount = count,
            Joined = alreadyExists
        };
    }
}
