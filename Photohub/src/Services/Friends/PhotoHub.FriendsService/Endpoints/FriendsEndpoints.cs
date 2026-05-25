using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        return group;
    }

    private static async Task<IResult> FollowAsync(
        [FromBody] FollowRequest request,
        FriendsDbContext dbContext,
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

        return Results.Ok(new FollowResponse(
            follow.Id,
            follow.FollowerId,
            follow.FollowingId,
            follow.CreatedAtUtc));
    }

    private static async Task<IResult> UnfollowAsync(
        [FromBody] FollowRequest request,
        FriendsDbContext dbContext,
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
}

public sealed record FollowRequest(
    Guid FollowerId,
    Guid FollowingId);

public sealed record FollowResponse(
    Guid Id,
    Guid FollowerId,
    Guid FollowingId,
    DateTime CreatedAtUtc);
