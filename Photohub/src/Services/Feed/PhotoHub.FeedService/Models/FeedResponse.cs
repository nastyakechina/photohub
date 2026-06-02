namespace PhotoHub.FeedService.Models;

public sealed record FeedResponse(
    IReadOnlyList<FeedItemResponse> Following,
    IReadOnlyList<RecommendedFeedItemResponse> Recommended,
    int TotalCount,
    int Page,
    int PageSize)
{
    // Backward-compatible alias for clients that read the flat list
    public IReadOnlyList<FeedItemResponse> Items => Following;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
}