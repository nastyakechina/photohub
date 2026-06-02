namespace PhotoHub.FeedService.Models;

public sealed record RecommendedFeedItemResponse(
    Guid PhotoId,
    Guid AuthorUserId,
    string AuthorUserName,
    string Title,
    string? Description,
    string ObjectKey,
    string? PreviewObjectKey,
    DateTime CreatedAtUtc,
    int MutualFriendsCount,
    string Reason);
