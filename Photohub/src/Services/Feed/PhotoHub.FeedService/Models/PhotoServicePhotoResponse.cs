namespace PhotoHub.FeedService.Models;

public sealed record PhotoServicePhotoResponse(
    Guid Id,
    Guid AuthorUserId,
    string Title,
    string? Description,
    string ObjectKey,
    string? PreviewObjectKey,
    DateTime CreatedAtUtc);
