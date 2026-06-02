namespace PhotoHub.LikeService.Application.Queries;

public sealed record HasUserLikedQuery(Guid PhotoId, Guid UserId);
