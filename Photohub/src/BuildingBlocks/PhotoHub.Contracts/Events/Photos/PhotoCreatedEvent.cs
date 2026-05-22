namespace PhotoHub.Contracts.Events.Photos;

/// <summary>
/// Published when a photo is uploaded and stored in object storage.
/// </summary>
public sealed record PhotoCreatedEvent(
    Guid PhotoId,
    Guid AuthorUserId,
    string OriginalFileName,
    string ObjectKey,
    DateTime CreatedAtUtc);
