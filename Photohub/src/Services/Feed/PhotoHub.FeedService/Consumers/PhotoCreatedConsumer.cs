using MassTransit;
using PhotoHub.Contracts.Events.Photos;
using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Domain.FeedItems;
using PhotoHub.FeedService.Infrastructure.Persistence;
using PhotoHub.FeedService.Services;

namespace PhotoHub.FeedService.Consumers;

public sealed class PhotoCreatedConsumer(
    ILogger<PhotoCreatedConsumer> logger,
    FeedDbContext dbContext,
    FriendsServiceClient friendsServiceClient,
    AuthServiceClient authServiceClient,
    RecommendationsService recommendationsService) : IConsumer<PhotoCreatedEvent>
{
    public async Task Consume(ConsumeContext<PhotoCreatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Processing PhotoCreatedEvent. PhotoId: {PhotoId}, AuthorUserId: {AuthorUserId}",
            message.PhotoId, message.AuthorUserId);

        IReadOnlyCollection<Guid> followers;
        try
        {
            followers = await friendsServiceClient.GetFollowersAsync(
                message.AuthorUserId, context.CancellationToken);
        }
        catch (FriendsServiceUnavailableException)
        {
            logger.LogWarning(
                "FriendsService unavailable while processing PhotoId {PhotoId}. Feed items not created.",
                message.PhotoId);
            return;
        }

        if (followers.Count == 0)
        {
            logger.LogInformation(
                "AuthorUserId {AuthorUserId} has no followers, skipping feed population.",
                message.AuthorUserId);
            return;
        }

        var userNames = await authServiceClient.GetUserNamesAsync(
            [message.AuthorUserId], context.CancellationToken);
        var authorName = userNames.GetValueOrDefault(message.AuthorUserId, "Пользователь");

        var feedItems = followers
            .Select(followerId => FeedItem.Create(
                followerId,
                message.PhotoId,
                message.AuthorUserId,
                authorName,
                message.Title,
                message.Description,
                message.ObjectKey,
                message.CreatedAtUtc))
            .ToList();

        await dbContext.FeedItems.AddRangeAsync(feedItems, context.CancellationToken);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "Created {Count} feed items for PhotoId {PhotoId}",
            feedItems.Count, message.PhotoId);

        // Invalidate recommendation caches for all followers — the author now has new content
        // so friends-of-followers might see the author in recommendations
        var invalidateTasks = followers.Select(followerId =>
            recommendationsService.InvalidateAsync(followerId));
        await Task.WhenAll(invalidateTasks);
    }
}
