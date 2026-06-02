using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Infrastructure.Persistence;

namespace PhotoHub.LikeService.Application.Queries;

public sealed class HasUserLikedQueryHandler(LikeDbContext dbContext)
{
    public async Task<bool> HandleAsync(
        HasUserLikedQuery query,
        CancellationToken cancellationToken)
    {
        return await dbContext.Likes.AnyAsync(
            like => like.PhotoId == query.PhotoId && like.UserId == query.UserId,
            cancellationToken);
    }
}
