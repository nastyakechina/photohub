using System.Net.Http.Json;
using System.Text.Json;

namespace PhotoHub.FeedService.Clients;

public class AuthServiceClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Dictionary<Guid, string>> GetUserNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idSet = userIds.ToHashSet();
            var result = new Dictionary<Guid, string>();
            var page = 1;
            int totalPages;

            do
            {
                var response = await httpClient.GetAsync(
                    $"/api/auth/users?page={page}&pageSize=50", cancellationToken);
                if (!response.IsSuccessStatusCode)
                    break;

                var data = await response.Content.ReadFromJsonAsync<PagedResponse>(JsonOptions, cancellationToken);
                if (data is null) break;

                totalPages = data.TotalPages;

                foreach (var u in data.Users.Where(u => idSet.Contains(u.UserId)))
                    result[u.UserId] = u.UserName;

                if (result.Count >= idSet.Count) break;
                page++;
            } while (page <= totalPages);

            return result;
        }
        catch
        {
            return new Dictionary<Guid, string>();
        }
    }

    public async Task<(List<UserDto> Users, int TotalPages)> GetUsersPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/auth/users?page={page}&pageSize={pageSize}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return ([], 0);

            var data = await response.Content.ReadFromJsonAsync<PagedResponse>(JsonOptions, cancellationToken);
            if (data is null) return ([], 0);

            return (data.Users, data.TotalPages);
        }
        catch
        {
            return ([], 0);
        }
    }

    public sealed record UserDto(Guid UserId, string UserName, string Email);
    private sealed record PagedResponse(List<UserDto> Users, int TotalPages);
}
