using MassTransit;
using PhotoHub.Contracts.Events.Photos;

namespace PhotoHub.PreviewService.Consumers;

public sealed class PhotoCreatedEventConsumer(ILogger<PhotoCreatedEventConsumer> logger)
    : IConsumer<PhotoCreatedEvent>
{
    public Task Consume(ConsumeContext<PhotoCreatedEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Received PhotoCreatedEvent. PhotoId: {PhotoId}, ObjectKey: {ObjectKey}, AuthorUserId: {AuthorUserId}",
            message.PhotoId,
            message.ObjectKey,
            message.AuthorUserId);

        return Task.CompletedTask;
    }
}
