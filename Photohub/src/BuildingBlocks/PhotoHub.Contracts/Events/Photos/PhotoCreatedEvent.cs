namespace PhotoHub.Contracts.Events.Photos;

public sealed record PhotoCreatedEvent(
    Guid PhotoId,
    Guid AuthorUserId,
    string Title,
    string ObjectKey,
    DateTime CreatedAtUtc,
    string? Description = null);
