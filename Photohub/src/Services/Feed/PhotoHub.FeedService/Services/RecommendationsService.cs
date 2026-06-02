using System.Text.Json;
using PhotoHub.FeedService.Clients;
using PhotoHub.FeedService.Models;
using StackExchange.Redis;

namespace PhotoHub.FeedService.Services;

public sealed class RecommendationsService(
    FriendsServiceClient friendsClient,
    PhotoServiceClient photoClient,
    AuthServiceClient authClient,
    IConnectionMultiplexer redis,
    ILogger<RecommendationsService> logger)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string CacheKey(Guid userId) => $"feed:recommended:{userId}";

    public async Task<List<RecommendedFeedItemResponse>> GetAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var db = redis.GetDatabase();
            var cached = await db.StringGetAsync(CacheKey(userId));
            if (cached.HasValue)
            {
                var result = JsonSerializer.Deserialize<List<RecommendedFeedItemResponse>>(cached!, JsonOptions);
                if (result is not null) return result;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for user {UserId}, computing recommendations without cache.", userId);
        }

        var (recommendations, shouldCache) = await ComputeAsync(userId, ct);

        if (shouldCache)
        {
            try
            {
                var db = redis.GetDatabase();
                var json = JsonSerializer.Serialize(recommendations, JsonOptions);
                await db.StringSetAsync(CacheKey(userId), json, CacheTtl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cache recommendations for user {UserId}.", userId);
            }
        }
        else
        {
            logger.LogInformation(
                "Recommendations for user {UserId} not cached due to high error rate during computation.", userId);
        }

        return recommendations;
    }

    public async Task InvalidateAsync(Guid userId)
    {
        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(CacheKey(userId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate recommendations cache for user {UserId}.", userId);
        }
    }

    private async Task<(List<RecommendedFeedItemResponse> Results, bool ShouldCache)> ComputeAsync(
        Guid userId, CancellationToken ct)
    {
        IReadOnlyCollection<Guid> myFollowing;
        try
        {
            myFollowing = await friendsClient.GetFollowingAsync(userId, ct);
        }
        catch (FriendsServiceUnavailableException)
        {
            return ([], false);
        }

        if (myFollowing.Count == 0) return ([], true);

        var myFollowingSet = myFollowing.ToHashSet();

        // Build map: candidateId → set of my friends who follow them
        var friendsWhoFollow = new Dictionary<Guid, HashSet<Guid>>();
        int friendLookupErrors = 0;

        foreach (var friendId in myFollowing)
        {
            IReadOnlyCollection<Guid> friendFollowing;
            try
            {
                friendFollowing = await friendsClient.GetFollowingAsync(friendId, ct);
            }
            catch (FriendsServiceUnavailableException)
            {
                friendLookupErrors++;
                continue;
            }

            foreach (var candidateId in friendFollowing)
            {
                if (candidateId == userId || myFollowingSet.Contains(candidateId))
                    continue;

                if (!friendsWhoFollow.TryGetValue(candidateId, out var set))
                {
                    set = [];
                    friendsWhoFollow[candidateId] = set;
                }
                set.Add(friendId);
            }
        }

        double errorRate = myFollowing.Count > 0
            ? (double)friendLookupErrors / myFollowing.Count
            : 0;
        bool shouldCache = errorRate <= 0.3;

        if (friendLookupErrors > 0)
        {
            logger.LogWarning(
                "Recommendations for user {UserId}: {Errors}/{Total} friend lookups failed (error rate {ErrorRate:P0}). Caching: {ShouldCache}",
                userId, friendLookupErrors, myFollowing.Count, errorRate, shouldCache);
        }

        if (friendsWhoFollow.Count == 0)
        {
            logger.LogInformation(
                "Recommendations for user {UserId}: 0 candidates found after {Total} friend lookups.",
                userId, myFollowing.Count);
            return ([], shouldCache);
        }

        // Batch-fetch all needed user names (candidates + mutual friends)
        var allNeededIds = friendsWhoFollow.Keys
            .Concat(friendsWhoFollow.Values.SelectMany(s => s))
            .Distinct();
        var userNames = await authClient.GetUserNamesAsync(allNeededIds, ct);

        // Fetch photos per candidate and build results
        var results = new List<RecommendedFeedItemResponse>();
        int candidatesWithPhotos = 0;
        int candidatesWithoutPhotos = 0;

        foreach (var (candidateId, mutualFriends) in friendsWhoFollow)
        {
            IReadOnlyCollection<PhotoServicePhotoResponse> photos;
            try
            {
                photos = await photoClient.GetPhotosByUserAsync(candidateId, ct);
            }
            catch
            {
                friendLookupErrors++;
                continue;
            }

            if (photos.Count == 0)
            {
                candidatesWithoutPhotos++;
                continue;
            }

            candidatesWithPhotos++;
            var authorName = userNames.GetValueOrDefault(candidateId, "Пользователь");
            var reason = BuildReason(mutualFriends, userNames);

            foreach (var photo in photos.OrderByDescending(p => p.CreatedAtUtc).Take(3))
            {
                results.Add(new RecommendedFeedItemResponse(
                    photo.Id,
                    candidateId,
                    authorName,
                    photo.Title,
                    photo.Description,
                    photo.ObjectKey,
                    photo.PreviewObjectKey,
                    photo.CreatedAtUtc,
                    mutualFriends.Count,
                    reason));
            }
        }

        logger.LogInformation(
            "Recommendations for user {UserId}: {CandidatesTotal} candidates, {WithPhotos} with photos, {WithoutPhotos} filtered (no photos). {ResultCount} items returned.",
            userId, friendsWhoFollow.Count, candidatesWithPhotos, candidatesWithoutPhotos, Math.Min(results.Count, 20));

        var sorted = results
            .OrderByDescending(r => r.MutualFriendsCount)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Take(20)
            .ToList();

        return (sorted, shouldCache);
    }

    private static string BuildReason(HashSet<Guid> mutualFriendIds, Dictionary<Guid, string> names)
    {
        var listed = mutualFriendIds
            .Where(names.ContainsKey)
            .Select(id => names[id])
            .Take(2)
            .ToList();

        if (listed.Count == 0) return "Возможно знакомы";

        var extra = mutualFriendIds.Count - listed.Count;
        var namesPart = string.Join(", ", listed);
        return extra > 0 ? $"Подписан(а) {namesPart} и ещё {extra}" : $"Подписан(а) {namesPart}";
    }
}