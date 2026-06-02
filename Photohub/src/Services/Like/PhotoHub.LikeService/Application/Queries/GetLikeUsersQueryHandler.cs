using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Infrastructure.Persistence;

namespace PhotoHub.LikeService.Application.Queries;

public class GetLikeUsersQueryHandler(LikeDbContext dbContext)
{
    public async Task<List<Guid>> HandleAsync(
        GetLikeUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Likes
            .Where(l => l.PhotoId == query.PhotoId)
            .Select(l => l.UserId)
            .ToListAsync(cancellationToken);
    }
}
