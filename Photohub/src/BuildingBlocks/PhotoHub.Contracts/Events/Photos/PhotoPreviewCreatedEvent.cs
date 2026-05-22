namespace PhotoHub.Contracts.Events.Photos;

/// <summary>
/// Published when a preview image is generated for an uploaded photo.
/// </summary>
public sealed record PhotoPreviewCreatedEvent(
    Guid PhotoId,
    string PreviewObjectKey,
    DateTime CreatedAtUtc);
