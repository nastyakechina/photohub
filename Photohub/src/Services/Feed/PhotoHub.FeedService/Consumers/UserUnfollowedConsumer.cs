using MassTransit;
using Microsoft.EntityFrameworkCore;
using PhotoHub.Contracts.Events.Friends;
using PhotoHub.FeedService.Infrastructure.Persistence;
using PhotoHub.FeedService.Services;

namespace PhotoHub.FeedService.Consumers;

public sealed class UserUnfollowedConsumer(
    ILogger<UserUnfollowedConsumer> logger,
    FeedDbContext dbContext,
    RecommendationsService recommendationsService) : IConsumer<UserUnfollowedEvent>
{
    public async Task Consume(ConsumeContext<UserUnfollowedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Processing UserUnfollowedEvent. FollowerUserId: {FollowerUserId}, FollowedUserId: {FollowedUserId}",
            message.FollowerUserId, message.FollowedUserId);

        var deleted = await dbContext.FeedItems
            .Where(fi => fi.UserId == message.FollowerUserId && fi.AuthorUserId == message.FollowedUserId)
            .ExecuteDeleteAsync(context.CancellationToken);

        logger.LogInformation(
            "Deleted {Count} feed items for FollowerUserId {FollowerUserId} from author {FollowedUserId}",
            deleted, message.FollowerUserId, message.FollowedUserId);

        // Follower unfollowed someone — their recommendations may now include that person
        await recommendationsService.InvalidateAsync(message.FollowerUserId);
    }
}
