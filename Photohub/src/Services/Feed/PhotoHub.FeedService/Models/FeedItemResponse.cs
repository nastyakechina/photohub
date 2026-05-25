namespace PhotoHub.FeedService.Models;

public sealed record FeedItemResponse(
    Guid PhotoId,
    Guid AuthorUserId,
    string AuthorUserName,
    string Title,
    string? Description,
    string ObjectKey,
    string? PreviewObjectKey,
    DateTime CreatedAtUtc);
