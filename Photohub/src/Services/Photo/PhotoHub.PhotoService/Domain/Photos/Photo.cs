namespace PhotoHub.PhotoService.Domain.Photos;

public sealed class Photo
{
    private Photo()
    {
    }

    private Photo(
        Guid id,
        Guid authorUserId,
        string title,
        string? description,
        string objectKey,
        DateTime createdAtUtc)
    {
        Id = id;
        AuthorUserId = authorUserId;
        Title = title;
        Description = description;
        ObjectKey = objectKey;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string ObjectKey { get; private set; } = string.Empty;

    public string? PreviewObjectKey { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Photo Create(Guid authorUserId, string title, string? description, string objectKey)
    {
        return new Photo(
            Guid.NewGuid(),
            authorUserId,
            title.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            objectKey.Trim(),
            DateTime.UtcNow);
    }

    public void SetPreviewObjectKey(string previewObjectKey)
    {
        PreviewObjectKey = previewObjectKey.Trim();
    }

    public void UpdateDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
