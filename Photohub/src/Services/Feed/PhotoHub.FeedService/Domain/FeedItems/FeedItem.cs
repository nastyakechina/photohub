namespace PhotoHub.FeedService.Domain.FeedItems;

public sealed class FeedItem
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PhotoId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string? PreviewObjectKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime AddedToFeedAtUtc { get; private set; }

    private FeedItem() { }

    public static FeedItem Create(
        Guid userId,
        Guid photoId,
        Guid authorUserId,
        string authorName,
        string title,
        string? description,
        string objectKey,
        DateTime createdAtUtc,
        string? previewObjectKey = null)
    {
        return new FeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PhotoId = photoId,
            AuthorUserId = authorUserId,
            AuthorName = authorName,
            Title = title,
            Description = description,
            ObjectKey = objectKey,
            PreviewObjectKey = previewObjectKey,
            CreatedAtUtc = createdAtUtc,
            AddedToFeedAtUtc = DateTime.UtcNow
        };
    }
}
