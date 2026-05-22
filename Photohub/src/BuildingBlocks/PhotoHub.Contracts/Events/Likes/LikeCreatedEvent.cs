namespace PhotoHub.Contracts.Events.Likes;

/// <summary>
/// Published when a user likes a photo.
/// </summary>
public sealed record LikeCreatedEvent(
    Guid LikeId,
    Guid PhotoId,
    Guid UserId,
    DateTime CreatedAtUtc);
