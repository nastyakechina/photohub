using Microsoft.AspNetCore.Mvc;
using PhotoHub.LikeService.Application.Commands;
using PhotoHub.LikeService.Application.Queries;

namespace PhotoHub.LikeService.Endpoints;

public static class LikeEndpoints
{
    public static RouteGroupBuilder MapLikeEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateLikeAsync).WithName("CreateLike");
        group.MapDelete("/", DeleteLikeAsync).WithName("DeleteLike");
        group.MapGet("/photos/{photoId:guid}/count", GetLikeCountAsync).WithName("GetPhotoLikeCount");
        group.MapGet("/photos/{photoId:guid}/users/{userId:guid}", HasUserLikedPhotoAsync).WithName("HasUserLikedPhoto");

        return group;
    }

    private static async Task<IResult> CreateLikeAsync(
        [FromBody] LikeRequest request,
        AddLikeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.PhotoId == Guid.Empty || request.UserId == Guid.Empty)
            return Results.BadRequest(new { error = "PhotoId and UserId are required." });

        var result = await handler.HandleAsync(
            new AddLikeCommand(request.PhotoId, request.UserId),
            cancellationToken);

        if (result is null)
            return Results.Conflict(new { error = "Like already exists." });

        return Results.Ok(new LikeResponse(result.Id, result.PhotoId, result.UserId, result.CreatedAtUtc));
    }

    private static async Task<IResult> DeleteLikeAsync(
        [FromBody] LikeRequest request,
        RemoveLikeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.PhotoId == Guid.Empty || request.UserId == Guid.Empty)
            return Results.BadRequest(new { error = "PhotoId and UserId are required." });

        var removed = await handler.HandleAsync(
            new RemoveLikeCommand(request.PhotoId, request.UserId),
            cancellationToken);

        return removed ? Results.NoContent() : Results.NotFound(new { error = "Like was not found." });
    }

    private static async Task<IResult> GetLikeCountAsync(
        Guid photoId,
        GetLikeCountQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty)
            return Results.BadRequest(new { error = "PhotoId is required." });

        var result = await handler.HandleAsync(new GetLikeCountQuery(photoId), cancellationToken);

        return Results.Ok(new LikeCountResponse(result.PhotoId, result.Count));
    }

    private static async Task<IResult> HasUserLikedPhotoAsync(
        Guid photoId,
        Guid userId,
        HasUserLikedQueryHandler handler,
        CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty || userId == Guid.Empty)
            return Results.BadRequest(new { error = "PhotoId and UserId are required." });

        var hasLiked = await handler.HandleAsync(
            new HasUserLikedQuery(photoId, userId),
            cancellationToken);

        return Results.Ok(hasLiked);
    }
}

public sealed record LikeRequest(Guid PhotoId, Guid UserId);
public sealed record LikeResponse(Guid Id, Guid PhotoId, Guid UserId, DateTime CreatedAtUtc);
public sealed record LikeCountResponse(Guid PhotoId, long Count);
