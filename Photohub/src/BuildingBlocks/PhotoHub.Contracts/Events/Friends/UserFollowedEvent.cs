namespace PhotoHub.Contracts.Events.Friends;

public sealed record UserFollowedEvent(
    Guid FollowerUserId,
    Guid FollowedUserId,
    DateTime FollowedAtUtc);