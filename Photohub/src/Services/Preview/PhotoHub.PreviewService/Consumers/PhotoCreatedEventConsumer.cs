using Amazon.S3;
using Amazon.S3.Model;
using MassTransit;
using PhotoHub.Contracts.Events.Photos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PhotoHub.PreviewService.Consumers;

public sealed class PhotoCreatedEventConsumer(
    ILogger<PhotoCreatedEventConsumer> logger,
    IAmazonS3 s3,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IConsumer<PhotoCreatedEvent>
{
    private const string Bucket = "photos";
    private const int MaxPreviewWidth = 400;

    public async Task Consume(ConsumeContext<PhotoCreatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Processing PhotoCreatedEvent. PhotoId: {PhotoId}, ObjectKey: {ObjectKey}",
            message.PhotoId, message.ObjectKey);

        try
        {
            // Download original from MinIO
            var getResponse = await s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = Bucket,
                Key = message.ObjectKey
            });

            using var originalMs = new MemoryStream();
            await getResponse.ResponseStream.CopyToAsync(originalMs);
            originalMs.Position = 0;

            // Resize
            using var image = await Image.LoadAsync(originalMs);
            if (image.Width > MaxPreviewWidth)
            {
                var ratio = (float)MaxPreviewWidth / image.Width;
                image.Mutate(x => x.Resize(MaxPreviewWidth, (int)(image.Height * ratio)));
            }

            // Upload preview
            var previewKey = $"previews/{message.ObjectKey}";
            using var previewMs = new MemoryStream();
            await image.SaveAsJpegAsync(previewMs);
            previewMs.Position = 0;

            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Bucket,
                Key = previewKey,
                InputStream = previewMs,
                ContentType = "image/jpeg",
                CannedACL = S3CannedACL.PublicRead,
            });

            // Notify PhotoService
            var photoServiceUrl = configuration["Services:PhotoServiceUrl"] ?? "http://localhost:5003";
            var http = httpClientFactory.CreateClient();
            var patchResponse = await http.PatchAsJsonAsync(
                $"{photoServiceUrl}/api/photos/{message.PhotoId}/preview",
                new { previewObjectKey = previewKey });

            if (patchResponse.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Preview created for PhotoId {PhotoId}: {PreviewKey}",
                    message.PhotoId, previewKey);
            }
            else
            {
                logger.LogWarning(
                    "Preview created but PhotoService update failed for PhotoId {PhotoId}. Status: {Status}",
                    message.PhotoId, patchResponse.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create preview for PhotoId {PhotoId}", message.PhotoId);
        }
    }
}
