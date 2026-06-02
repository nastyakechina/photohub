using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Domain.Likes;
using PhotoHub.LikeService.Infrastructure.Persistence;
using PhotoHub.LikeService.Infrastructure.Redis;

namespace PhotoHub.LikeService.Application.Commands;

public sealed class AddLikeCommandHandler(
    LikeDbContext dbContext,
    LikeCounterCache counterCache)
{
    public async Task<AddLikeResult?> HandleAsync(
        AddLikeCommand command,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Likes.AnyAsync(
            like => like.PhotoId == command.PhotoId && like.UserId == command.UserId,
            cancellationToken);

        if (exists) return null;

        var like = Like.Create(command.PhotoId, command.UserId);

        dbContext.Likes.Add(like);
        await dbContext.SaveChangesAsync(cancellationToken);
        await counterCache.IncrementAsync(like.PhotoId);

        return new AddLikeResult(like.Id, like.PhotoId, like.UserId, like.CreatedAtUtc);
    }
}
