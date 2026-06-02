using System.Net.Http.Json;

namespace PhotoHub.FeedService.Clients;

public sealed class FriendsServiceClient(HttpClient httpClient)
{
    public async Task<IReadOnlyCollection<Guid>> GetFollowingAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await GetUserListAsync($"/api/friends/{userId}/following", cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetFollowersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await GetUserListAsync($"/api/friends/{userId}/followers", cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> GetUserListAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new FriendsServiceUnavailableException();

            var ids = await response.Content.ReadFromJsonAsync<List<Guid>>(cancellationToken);
            return ids ?? [];
        }
        catch (HttpRequestException)
        {
            throw new FriendsServiceUnavailableException();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new FriendsServiceUnavailableException();
        }
    }
}

public sealed class FriendsServiceUnavailableException : Exception;
