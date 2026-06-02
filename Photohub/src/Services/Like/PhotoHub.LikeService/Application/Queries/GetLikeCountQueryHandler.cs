using PhotoHub.LikeService.Infrastructure.Persistence;
using PhotoHub.LikeService.Infrastructure.Redis;

namespace PhotoHub.LikeService.Application.Queries;

public sealed class GetLikeCountQueryHandler(
    LikeDbContext dbContext,
    LikeCounterCache counterCache)
{
    public async Task<GetLikeCountResult> HandleAsync(
        GetLikeCountQuery query,
        CancellationToken cancellationToken)
    {
        var count = await counterCache.GetOrRefreshAsync(
            query.PhotoId,
            dbContext,
            cancellationToken);

        return new GetLikeCountResult(query.PhotoId, count);
    }
}
