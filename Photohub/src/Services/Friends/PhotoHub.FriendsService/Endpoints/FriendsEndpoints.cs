using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoHub.Contracts.Events.Friends;
using PhotoHub.FriendsService.Domain.Follows;
using PhotoHub.FriendsService.Infrastructure.Persistence;

namespace PhotoHub.FriendsService.Endpoints;

public static class FriendsEndpoints
{
    public static RouteGroupBuilder MapFriendsEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/follow", FollowAsync)
            .WithName("FollowUser");

        group.MapDelete("/follow", UnfollowAsync)
            .WithName("UnfollowUser");

        group.MapGet("/{userId:guid}/following", GetFollowingAsync)
            .WithName("GetFollowing");

        group.MapGet("/{userId:guid}/followers", GetFollowersAsync)
            .WithName("GetFollowers");

        group.MapGet("/{userId:guid}/following/{targetUserId:guid}", IsFollowingAsync)
            .WithName("IsFollowing");

        group.MapGet("/{userId:guid}/people-scores", GetPeopleScoresAsync)
            .WithName("GetPeopleScores");

        return group;
    }

    private static async Task<IResult> FollowAsync(
        [FromBody] FollowRequest request,
        FriendsDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        if (request.FollowerId == Guid.Empty || request.FollowingId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "FollowerId and FollowingId are required." });
        }

        if (request.FollowerId == request.FollowingId)
        {
            return Results.BadRequest(new { error = "User cannot follow themselves." });
        }

        var exists = await dbContext.Follows.AnyAsync(
            follow => follow.FollowerId == request.FollowerId && follow.FollowingId == request.FollowingId,
            cancellationToken);

        if (exists)
        {
            return Results.Conflict(new { error = "Follow relation already exists." });
        }

        var follow = Follow.Create(request.FollowerId, request.FollowingId);

        dbContext.Follows.Add(follow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(new UserFollowedEvent(
            request.FollowerId,
            request.FollowingId,
            DateTime.UtcNow), cancellationToken);

        return Results.Ok(new FollowResponse(
            follow.Id,
            follow.FollowerId,
            follow.FollowingId,
            follow.CreatedAtUtc));
    }

    private static async Task<IResult> UnfollowAsync(
        [FromBody] FollowRequest request,
        FriendsDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        CancellationToken cancellationToken)
    {
        if (request.FollowerId == Guid.Empty || request.FollowingId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "FollowerId and FollowingId are required." });
        }

        if (request.FollowerId == request.FollowingId)
        {
            return Results.BadRequest(new { error = "User cannot unfollow themselves." });
        }

        var follow = await dbContext.Follows.FirstOrDefaultAsync(
            item => item.FollowerId == request.FollowerId && item.FollowingId == request.FollowingId,
            cancellationToken);

        if (follow is null)
        {
            return Results.NotFound(new { error = "Follow relation was not found." });
        }

        dbContext.Follows.Remove(follow);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(new UserUnfollowedEvent(
            request.FollowerId,
            request.FollowingId,
            DateTime.UtcNow), cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> GetFollowingAsync(
        Guid userId,
        FriendsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UserId is required." });
        }

        var followingIds = await dbContext.Follows
            .Where(follow => follow.FollowerId == userId)
            .OrderBy(follow => follow.CreatedAtUtc)
            .Select(follow => follow.FollowingId)
            .ToListAsync(cancellationToken);

        return Results.Ok(followingIds);
    }

    private static async Task<IResult> GetFollowersAsync(
        Guid userId,
        FriendsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UserId is required." });
        }

        var followerIds = await dbContext.Follows
            .Where(follow => follow.FollowingId == userId)
            .OrderBy(follow => follow.CreatedAtUtc)
            .Select(follow => follow.FollowerId)
            .ToListAsync(cancellationToken);

        return Results.Ok(followerIds);
    }

    private static async Task<IResult> IsFollowingAsync(
        Guid userId,
        Guid targetUserId,
        FriendsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || targetUserId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UserId and TargetUserId are required." });
        }

        var isFollowing = await dbContext.Follows.AnyAsync(
            follow => follow.FollowerId == userId && follow.FollowingId == targetUserId,
            cancellationToken);

        return Results.Ok(isFollowing);
    }

    private static async Task<IResult> GetPeopleScoresAsync(
        Guid userId,
        FriendsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UserId is required." });
        }

        var allFollows = await dbContext.Follows.ToListAsync(cancellationToken);

        var myFollowing = allFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowingId)
            .ToHashSet();

        var myFollowers = allFollows
            .Where(f => f.FollowingId == userId)
            .Select(f => f.FollowerId)
            .ToHashSet();

        var allUserIds = allFollows
            .SelectMany(f => new[] { f.FollowerId, f.FollowingId })
            .Where(id => id != userId)
            .ToHashSet();

        var scores = allUserIds.Select(targetId =>
        {
            var isFollowing = myFollowing.Contains(targetId);

            var targetFollowing = allFollows
                .Where(f => f.FollowerId == targetId)
                .Select(f => f.FollowingId)
                .ToHashSet();

            var targetFollowers = allFollows
                .Where(f => f.FollowingId == targetId)
                .Select(f => f.FollowerId)
                .ToHashSet();

            var commonFollowing = myFollowing.Intersect(targetFollowing).Count();
            var commonFollowers = myFollowers.Intersect(targetFollowers).Count();
            var commonScore = commonFollowing + commonFollowers;

            var isMyFollower = myFollowers.Contains(targetId);
            var effectiveScore = !isFollowing && isMyFollower && commonScore == 0 ? 1 : commonScore;

            var section = isFollowing ? "following"
                : effectiveScore > 0 ? "suggested"
                : "others";

            return new PeopleScoreDto(targetId, isFollowing, effectiveScore, section);
        }).ToList();

        return Results.Ok(scores);
    }
}

public sealed record FollowRequest(
    Guid FollowerId,
    Guid FollowingId);

public sealed record FollowResponse(
    Guid Id,
    Guid FollowerId,
    Guid FollowingId,
    DateTime CreatedAtUtc);

public sealed record PeopleScoreDto(
    Guid UserId,
    bool IsFollowing,
    int CommonScore,
    string Section);
