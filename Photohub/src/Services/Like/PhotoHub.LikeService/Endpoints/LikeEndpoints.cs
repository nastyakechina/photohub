using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Domain.Likes;
using PhotoHub.LikeService.Infrastructure.Persistence;
using PhotoHub.LikeService.Infrastructure.Redis;

namespace PhotoHub.LikeService.Endpoints;

public static class LikeEndpoints
{
    public static RouteGroupBuilder MapLikeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateLikeAsync)
            .WithName("CreateLike");

        group.MapDelete("/", DeleteLikeAsync)
            .WithName("DeleteLike");

        group.MapGet("/photos/{photoId:guid}/count", GetLikeCountAsync)
            .WithName("GetPhotoLikeCount");

        group.MapGet("/photos/{photoId:guid}/users/{userId:guid}", HasUserLikedPhotoAsync)
            .WithName("HasUserLikedPhoto");

        return group;
    }

    private static async Task<IResult> CreateLikeAsync(
        LikeRequest request,
        LikeDbContext dbContext,
        LikeCounterCache counterCache,
        CancellationToken cancellationToken)
    {
        if (request.PhotoId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PhotoId and UserId are required." });
        }

        var exists = await dbContext.Likes.AnyAsync(
            like => like.PhotoId == request.PhotoId && like.UserId == request.UserId,
            cancellationToken);

        if (exists)
        {
            return Results.Conflict(new { error = "Like already exists." });
        }

        var like = Like.Create(request.PhotoId, request.UserId);

        dbContext.Likes.Add(like);
        await dbContext.SaveChangesAsync(cancellationToken);
        await counterCache.IncrementAsync(like.PhotoId);

        return Results.Ok(new LikeResponse(
            like.Id,
            like.PhotoId,
            like.UserId,
            like.CreatedAtUtc));
    }

    private static async Task<IResult> DeleteLikeAsync(
        LikeRequest request,
        LikeDbContext dbContext,
        LikeCounterCache counterCache,
        CancellationToken cancellationToken)
    {
        if (request.PhotoId == Guid.Empty || request.UserId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PhotoId and UserId are required." });
        }

        var like = await dbContext.Likes.FirstOrDefaultAsync(
            item => item.PhotoId == request.PhotoId && item.UserId == request.UserId,
            cancellationToken);

        if (like is null)
        {
            return Results.NotFound(new { error = "Like was not found." });
        }

        dbContext.Likes.Remove(like);
        await dbContext.SaveChangesAsync(cancellationToken);
        await counterCache.DecrementAsync(request.PhotoId);

        return Results.NoContent();
    }

    private static async Task<IResult> GetLikeCountAsync(
        Guid photoId,
        LikeDbContext dbContext,
        LikeCounterCache counterCache,
        CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PhotoId is required." });
        }

        var count = await counterCache.GetOrRefreshAsync(photoId, dbContext, cancellationToken);

        return Results.Ok(new LikeCountResponse(photoId, count));
    }

    private static async Task<IResult> HasUserLikedPhotoAsync(
        Guid photoId,
        Guid userId,
        LikeDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty || userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PhotoId and UserId are required." });
        }

        var hasLiked = await dbContext.Likes.AnyAsync(
            like => like.PhotoId == photoId && like.UserId == userId,
            cancellationToken);

        return Results.Ok(hasLiked);
    }
}

public sealed record LikeRequest(
    Guid PhotoId,
    Guid UserId);

public sealed record LikeResponse(
    Guid Id,
    Guid PhotoId,
    Guid UserId,
    DateTime CreatedAtUtc);

public sealed record LikeCountResponse(
    Guid PhotoId,
    long Count);
