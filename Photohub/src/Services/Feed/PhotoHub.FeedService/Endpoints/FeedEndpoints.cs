using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Models;

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
        FriendsServiceClient friendsServiceClient,
        PhotoServiceClient photoServiceClient,
        AuthServiceClient authServiceClient,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UserId is required." });
        }

        IReadOnlyCollection<Guid> followingUserIds;

        try
        {
            followingUserIds = await friendsServiceClient.GetFollowingAsync(userId, cancellationToken);
        }
        catch (FriendsServiceUnavailableException)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var photoTasks = followingUserIds.Select(followingUserId =>
            photoServiceClient.GetPhotosByUserAsync(followingUserId, cancellationToken));

        var photoResults = await Task.WhenAll(photoTasks);

        var authorIds = photoResults.SelectMany(p => p).Select(p => p.AuthorUserId).Distinct();
        var userNames = await authServiceClient.GetUserNamesAsync(authorIds, cancellationToken);

        var feed = photoResults
            .SelectMany(photos => photos)
            .OrderByDescending(photo => photo.CreatedAtUtc)
            .Take(50)
            .Select(photo => new FeedItemResponse(
                photo.Id,
                photo.AuthorUserId,
                userNames.GetValueOrDefault(photo.AuthorUserId, "Пользователь"),
                photo.Title,
                photo.Description,
                photo.ObjectKey,
                photo.PreviewObjectKey,
                photo.CreatedAtUtc))
            .ToList();

        return Results.Ok(feed);
    }
}
