using System.Net.Http.Json;
using PhotoHub.FeedService.Models;

namespace PhotoHub.FeedService.Clients;

public sealed class PhotoServiceClient(HttpClient httpClient)
{
    public async Task<IReadOnlyCollection<PhotoServicePhotoResponse>> GetPhotosByUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/photos/by-user/{userId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var photos = await response.Content.ReadFromJsonAsync<List<PhotoServicePhotoResponse>>(
                cancellationToken);

            return photos ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
    }
}
