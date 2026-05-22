namespace PhotoHub.Contracts.Events.Likes;

/// <summary>
/// Published when a user removes a like from a photo.
/// </summary>
public sealed record LikeRemovedEvent(
    Guid PhotoId,
    Guid UserId,
    DateTime RemovedAtUtc);
