namespace PhotoHub.LikeService.Application.Commands;

public sealed record RemoveLikeCommand(Guid PhotoId, Guid UserId);
