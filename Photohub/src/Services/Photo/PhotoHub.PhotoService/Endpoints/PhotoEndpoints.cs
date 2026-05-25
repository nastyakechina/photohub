using Amazon.S3;
using Amazon.S3.Model;
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
            .WithName("CreatePhoto")
            .DisableAntiforgery();

        group.MapGet("/{photoId:guid}", GetPhotoAsync)
            .WithName("GetPhoto");

        group.MapGet("/by-user/{userId:guid}", GetPhotosByUserAsync)
            .WithName("GetPhotosByUser");

        group.MapPatch("/{photoId:guid}/preview", UpdatePreviewAsync)
            .WithName("UpdatePhotoPreview");

        return group;
    }

    private static async Task<IResult> CreatePhotoAsync(
        HttpContext context,
        IAmazonS3 s3,
        PhotoDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        IFormCollection form;
        try { form = await context.Request.ReadFormAsync(cancellationToken); }
        catch { return Results.BadRequest(new { error = "Expected multipart/form-data." }); }

        var authorUserIdStr = form["authorUserId"].ToString();
        var title = form["title"].ToString();
        var description = form["description"].ToString();
        var file = form.Files["file"];

        if (!Guid.TryParse(authorUserIdStr, out var authorUserId) || authorUserId == Guid.Empty)
            return Results.BadRequest(new { error = "authorUserId is required." });

        if (string.IsNullOrWhiteSpace(title))
            return Results.BadRequest(new { error = "title is required." });

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectKey = $"{authorUserId}/{Guid.NewGuid()}{ext}";

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = "photos",
            Key = objectKey,
            InputStream = ms,
            ContentType = file.ContentType,
            CannedACL = S3CannedACL.PublicRead,
        }, cancellationToken);

        var photo = Photo.Create(authorUserId, title,
            string.IsNullOrWhiteSpace(description) ? null : description,
            objectKey);

        dbContext.Photos.Add(photo);
        await dbContext.SaveChangesAsync(cancellationToken);

        var logger = loggerFactory.CreateLogger("PhotoCreatedPublisher");
        var photoCreatedEvent = new PhotoCreatedEvent(
            photo.Id, photo.AuthorUserId, photo.Title, photo.ObjectKey, photo.CreatedAtUtc);
        await publishEndpoint.Publish(photoCreatedEvent, cancellationToken);

        logger.LogInformation(
            "Published PhotoCreatedEvent for PhotoId {PhotoId}, ObjectKey {ObjectKey}, AuthorUserId {AuthorUserId}",
            photo.Id, photo.ObjectKey, photo.AuthorUserId);

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
