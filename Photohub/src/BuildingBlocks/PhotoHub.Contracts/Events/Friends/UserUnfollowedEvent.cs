namespace PhotoHub.Contracts.Events.Friends;

public sealed record UserUnfollowedEvent(
    Guid FollowerUserId,
    Guid FollowedUserId,
    DateTime UnfollowedAtUtc);
