namespace PhotoHub.FriendsService.Domain.Follows;

public sealed class Follow
{
    private Follow()
    {
    }

    private Follow(Guid id, Guid followerId, Guid followingId, DateTime createdAtUtc)
    {
        Id = id;
        FollowerId = followerId;
        FollowingId = followingId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid FollowerId { get; private set; }

    public Guid FollowingId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Follow Create(Guid followerId, Guid followingId)
    {
        if (followerId == followingId)
        {
            throw new ArgumentException("FollowerId cannot be equal to FollowingId.");
        }

        return new Follow(
            Guid.NewGuid(),
            followerId,
            followingId,
            DateTime.UtcNow);
    }
}
