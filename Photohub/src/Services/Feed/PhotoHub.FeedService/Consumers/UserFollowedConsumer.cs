using MassTransit;
using PhotoHub.Contracts.Events.Friends;
using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Domain.FeedItems;
using PhotoHub.FeedService.Infrastructure.Persistence;
using PhotoHub.FeedService.Services;

namespace PhotoHub.FeedService.Consumers;

public sealed class UserFollowedConsumer(
    ILogger<UserFollowedConsumer> logger,
    FeedDbContext dbContext,
    PhotoServiceClient photoClient,
    AuthServiceClient authClient,
    RecommendationsService recommendationsService) : IConsumer<UserFollowedEvent>
{
    public async Task Consume(ConsumeContext<UserFollowedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Processing UserFollowedEvent. FollowerUserId: {FollowerUserId}, FollowedUserId: {FollowedUserId}",
            message.FollowerUserId, message.FollowedUserId);

        var photos = await photoClient.GetPhotosByUserAsync(message.FollowedUserId, context.CancellationToken);

        if (photos.Count == 0)
        {
            logger.LogInformation(
                "FollowedUserId {FollowedUserId} has no photos, nothing to backfill.",
                message.FollowedUserId);
            await recommendationsService.InvalidateAsync(message.FollowerUserId);
            return;
        }

        var userNames = await authClient.GetUserNamesAsync(
            [message.FollowedUserId], context.CancellationToken);
        var authorName = userNames.GetValueOrDefault(message.FollowedUserId, "Пользователь");

        var feedItems = photos
            .Select(photo => FeedItem.Create(
                message.FollowerUserId,
                photo.Id,
                message.FollowedUserId,
                authorName,
                photo.Title,
                photo.Description,
                photo.ObjectKey,
                photo.CreatedAtUtc,
                photo.PreviewObjectKey))
            .ToList();

        await dbContext.FeedItems.AddRangeAsync(feedItems, context.CancellationToken);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // Invalidate recommendation cache — this person is no longer a recommendation candidate
        await recommendationsService.InvalidateAsync(message.FollowerUserId);

        logger.LogInformation(
            "Backfilled {Count} feed items for FollowerUserId {FollowerUserId} from author {FollowedUserId}",
            feedItems.Count, message.FollowerUserId, message.FollowedUserId);
    }
}