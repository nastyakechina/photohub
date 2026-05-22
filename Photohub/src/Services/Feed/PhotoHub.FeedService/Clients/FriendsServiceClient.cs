using System.Net.Http.Json;

namespace PhotoHub.FeedService.Clients;

public sealed class FriendsServiceClient(HttpClient httpClient)
{
    public async Task<IReadOnlyCollection<Guid>> GetFollowingAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/friends/{userId}/following",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new FriendsServiceUnavailableException();
            }

            var following = await response.Content.ReadFromJsonAsync<List<Guid>>(
                cancellationToken);

            return following ?? [];
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
