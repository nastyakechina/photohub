using Microsoft.EntityFrameworkCore;
using PhotoHub.FeedService.Infrastructure.Persistence;
using PhotoHub.FeedService.Models;
using PhotoHub.FeedService.Services;

namespace PhotoHub.FeedService.Endpoints;

public static class FeedEndpoints
{
    public static RouteGroupBuilder MapFeedEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{userId:guid}", GetFeedAsync)
            .WithName("GetUserFeed");

        return group;
    }

    private static async Task<IResult> GetFeedAsync(
        Guid userId,
        FeedDbContext dbContext,
        RecommendationsService recommendationsService,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            return Results.BadRequest(new { error = "UserId is required." });

        var followingTask = dbContext.FeedItems
            .Where(fi => fi.UserId == userId)
            .OrderByDescending(fi => fi.CreatedAtUtc)
            .Take(50)
            .Select(fi => new FeedItemResponse(
                fi.PhotoId,
                fi.AuthorUserId,
                fi.AuthorName,
                fi.Title,
                fi.Description,
                fi.ObjectKey,
                fi.PreviewObjectKey,
                fi.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var recommendedTask = recommendationsService.GetAsync(userId, cancellationToken);

        await Task.WhenAll(followingTask, recommendedTask);

        return Results.Ok(new FeedResponse(followingTask.Result, recommendedTask.Result));
    }
}
