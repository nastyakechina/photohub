using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Infrastructure.Persistence;
using PhotoHub.LikeService.Infrastructure.Redis;

namespace PhotoHub.LikeService.Application.Commands;

public sealed class RemoveLikeCommandHandler(
    LikeDbContext dbContext,
    LikeCounterCache counterCache)
{
    public async Task<bool> HandleAsync(
        RemoveLikeCommand command,
        CancellationToken cancellationToken)
    {
        var like = await dbContext.Likes.FirstOrDefaultAsync(
            item => item.PhotoId == command.PhotoId && item.UserId == command.UserId,
            cancellationToken);

        if (like is null) return false;

        dbContext.Likes.Remove(like);
        await dbContext.SaveChangesAsync(cancellationToken);
        await counterCache.DecrementAsync(command.PhotoId);

        return true;
    }
}
