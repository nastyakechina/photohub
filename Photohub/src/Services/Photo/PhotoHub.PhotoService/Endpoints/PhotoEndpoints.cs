using MassTransit;
using Microsoft.EntityFrameworkCore;
using PhotoHub.Contracts.Events.Photos;
using PhotoHub.PhotoService.Domain.Photos;
using PhotoHub.PhotoService.Infrastructure.Persistence;

namespace PhotoHub.PhotoService.Endpoints;

public static class PhotoEndpoints
{
    public static RouteGroupBuilder MapPhotoEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreatePhotoAsync)
            .WithName("CreatePhoto");

        group.MapGet("/{photoId:guid}", GetPhotoAsync)
            .WithName("GetPhoto");

        group.MapGet("/by-user/{userId:guid}", GetPhotosByUserAsync)
            .WithName("GetPhotosByUser");

        group.MapPatch("/{photoId:guid}/preview", UpdatePreviewAsync)
            .WithName("UpdatePhotoPreview");

        return group;
    }

    private static async Task<IResult> CreatePhotoAsync(
        CreatePhotoRequest request,
        PhotoDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (request.AuthorUserId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "AuthorUserId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { error = "Title is required." });
        }

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
        {
            return Results.BadRequest(new { error = "ObjectKey is required." });
        }

        var photo = Photo.Create(
            request.AuthorUserId,
            request.Title,
            request.Description,
            request.ObjectKey);

        dbContext.Photos.Add(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        var logger = loggerFactory.CreateLogger("PhotoCreatedPublisher");
        var photoCreatedEvent = new PhotoCreatedEvent(
            photo.Id,
            photo.AuthorUserId,
            photo.Title,
            photo.ObjectKey,
            photo.CreatedAtUtc);

        await publishEndpoint.Publish(photoCreatedEvent, cancellationToken);

        logger.LogInformation(
            "Published PhotoCreatedEvent for PhotoId {PhotoId}, ObjectKey {ObjectKey}, AuthorUserId {AuthorUserId}",
            photo.Id,
            photo.ObjectKey,
            photo.AuthorUserId);

        return Results.Ok(new CreatePhotoResponse(photo.Id));
    }

    private static async Task<IResult> GetPhotoAsync(
        Guid photoId,
        PhotoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PhotoId is required." });
        }

        var photo = await dbContext.Photos
            .AsNoTracking()
            .Where(item => item.Id == photoId)
            .Select(item => new PhotoResponse(
                item.Id,
                item.AuthorUserId,
                item.Title,
                item.Description,
                item.ObjectKey,
                item.PreviewObjectKey,
                item.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return photo is null
            ? Results.NotFound(new { error = "Photo was not found." })
            : Results.Ok(photo);
    }

    private static async Task<IResult> GetPhotosByUserAsync(
        Guid userId,
        PhotoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "UserId is required." });
        }

        var photos = await dbContext.Photos
            .AsNoTracking()
            .Where(photo => photo.AuthorUserId == userId)
            .OrderByDescending(photo => photo.CreatedAtUtc)
            .Select(photo => new PhotoResponse(
                photo.Id,
                photo.AuthorUserId,
                photo.Title,
                photo.Description,
                photo.ObjectKey,
                photo.PreviewObjectKey,
                photo.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(photos);
    }

    private static async Task<IResult> UpdatePreviewAsync(
        Guid photoId,
        UpdatePhotoPreviewRequest request,
        PhotoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (photoId == Guid.Empty)
        {
            return Results.BadRequest(new { error = "PhotoId is required." });
        }

        if (string.IsNullOrWhiteSpace(request.PreviewObjectKey))
        {
            return Results.BadRequest(new { error = "PreviewObjectKey is required." });
        }

        var photo = await dbContext.Photos.FirstOrDefaultAsync(
            item => item.Id == photoId,
            cancellationToken);

        if (photo is null)
        {
            return Results.NotFound(new { error = "Photo was not found." });
        }

        photo.SetPreviewObjectKey(request.PreviewObjectKey);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

public sealed record CreatePhotoRequest(
    Guid AuthorUserId,
    string Title,
    string? Description,
    string ObjectKey);

public sealed record CreatePhotoResponse(Guid PhotoId);

public sealed record UpdatePhotoPreviewRequest(string PreviewObjectKey);

public sealed record PhotoResponse(
    Guid Id,
    Guid AuthorUserId,
    string Title,
    string? Description,
    string ObjectKey,
    string? PreviewObjectKey,
    DateTime CreatedAtUtc);
