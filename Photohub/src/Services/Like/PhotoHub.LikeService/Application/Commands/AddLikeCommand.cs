namespace PhotoHub.LikeService.Application.Commands;

public sealed record AddLikeCommand(Guid PhotoId, Guid UserId);

public sealed record AddLikeResult(Guid Id, Guid PhotoId, Guid UserId, DateTime CreatedAtUtc);
