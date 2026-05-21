namespace PhotoHub.LikeService.Domain.Likes;

public sealed class Like
{
    private Like()
    {
    }

    private Like(Guid id, Guid photoId, Guid userId, DateTime createdAtUtc)
    {
        Id = id;
        PhotoId = photoId;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid PhotoId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Like Create(Guid photoId, Guid userId)
    {
        return new Like(
            Guid.NewGuid(),
            photoId,
            userId,
            DateTime.UtcNow);
    }
}
