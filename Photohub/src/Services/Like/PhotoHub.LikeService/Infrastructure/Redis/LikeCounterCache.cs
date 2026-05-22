using Microsoft.EntityFrameworkCore;
using PhotoHub.LikeService.Infrastructure.Persistence;
using StackExchange.Redis;

namespace PhotoHub.LikeService.Infrastructure.Redis;

public sealed class LikeCounterCache(IConnectionMultiplexer redis)
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task IncrementAsync(Guid photoId)
    {
        await _database.StringIncrementAsync(GetKey(photoId));
    }

    public async Task DecrementAsync(Guid photoId)
    {
        var count = await _database.StringDecrementAsync(GetKey(photoId));

        if (count < 0)
        {
            await _database.StringSetAsync(GetKey(photoId), 0);
        }
    }

    public async Task<long> GetOrRefreshAsync(
        Guid photoId,
        LikeDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var key = GetKey(photoId);
        var cachedCount = await _database.StringGetAsync(key);

        if (cachedCount.HasValue && long.TryParse(cachedCount.ToString(), out var count))
        {
            return count;
        }

        var actualCount = await dbContext.Likes
            .LongCountAsync(like => like.PhotoId == photoId, cancellationToken);

        await _database.StringSetAsync(key, actualCount);

        return actualCount;
    }

    private static string GetKey(Guid photoId)
    {
        return $"likes:photo:{photoId}:count";
    }
}
