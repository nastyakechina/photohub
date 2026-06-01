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
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 10)
    {
        if (userId == Guid.Empty)
            return Results.BadRequest(new { error = "UserId is required." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = dbContext.FeedItems.Where(fi => fi.UserId == userId);

        // Kick off recommendations in parallel (pure HTTP calls, no shared DbContext)
        var recommendedTask = page == 1
            ? recommendationsService.GetAsync(userId, cancellationToken)
            : Task.FromResult<List<RecommendedFeedItemResponse>>([]);

        // EF Core: two queries sequential on the same DbContext instance
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var cutoff = DateTime.UtcNow.AddHours(-1);
        var following = await baseQuery
            .OrderByDescending(fi => fi.AddedToFeedAtUtc > cutoff ? DateTime.MaxValue : fi.CreatedAtUtc)
            .ThenByDescending(fi => fi.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        var recommended = await recommendedTask;

        return Results.Ok(new FeedResponse(following, recommended, totalCount, page, pageSize));
    }
}
