using Microsoft.EntityFrameworkCore;
using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Domain.FeedItems;
using PhotoHub.FeedService.Infrastructure.Persistence;

namespace PhotoHub.FeedService.BackgroundServices;

public sealed class FeedBackfillService(
    IServiceProvider serviceProvider,
    ILogger<FeedBackfillService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for dependent services to be ready
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FeedDbContext>();
        var photosClient = scope.ServiceProvider.GetRequiredService<PhotoServiceClient>();
        var friendsClient = scope.ServiceProvider.GetRequiredService<FriendsServiceClient>();
        var authClient = scope.ServiceProvider.GetRequiredService<AuthServiceClient>();

        var existingCount = await db.FeedItems.CountAsync(stoppingToken);
        if (existingCount > 0)
        {
            logger.LogInformation("Feed backfill skipped: {Count} items already exist.", existingCount);
            return;
        }

        logger.LogInformation("Starting historical feed backfill.");

        var page = 1;
        var totalFeedItems = 0;
        int totalPages;

        do
        {
            var (users, pages) = await authClient.GetUsersPageAsync(page, 50, stoppingToken);
            totalPages = pages;

            if (users.Count == 0) break;

            foreach (var user in users)
            {
                if (stoppingToken.IsCancellationRequested) return;

                var photos = await photosClient.GetPhotosByUserAsync(user.UserId, stoppingToken);
                if (photos.Count == 0) continue;

                IReadOnlyCollection<Guid> followers;
                try
                {
                    followers = await friendsClient.GetFollowersAsync(user.UserId, stoppingToken);
                }
                catch (FriendsServiceUnavailableException)
                {
                    continue;
                }

                if (followers.Count == 0) continue;

                var feedItems = new List<FeedItem>(photos.Count * followers.Count);

                foreach (var photo in photos)
                {
                    foreach (var followerId in followers)
                    {
                        feedItems.Add(FeedItem.Create(
                            followerId,
                            photo.Id,
                            user.UserId,
                            user.UserName,
                            photo.Title,
                            photo.Description,
                            photo.ObjectKey,
                            photo.CreatedAtUtc,
                            photo.PreviewObjectKey));
                    }
                }

                await db.FeedItems.AddRangeAsync(feedItems, stoppingToken);
                await db.SaveChangesAsync(stoppingToken);
                totalFeedItems += feedItems.Count;

                logger.LogInformation(
                    "Backfill: created {Count} feed items for author {UserName} ({AuthorId})",
                    feedItems.Count, user.UserName, user.UserId);
            }

            page++;
        } while (page <= totalPages);

        logger.LogInformation("Feed backfill complete. Total feed items created: {Total}.", totalFeedItems);
    }
}
