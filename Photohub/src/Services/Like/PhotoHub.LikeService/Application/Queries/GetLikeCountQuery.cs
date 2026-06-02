namespace PhotoHub.LikeService.Application.Queries;

public sealed record GetLikeCountQuery(Guid PhotoId);

public sealed record GetLikeCountResult(Guid PhotoId, long Count);
