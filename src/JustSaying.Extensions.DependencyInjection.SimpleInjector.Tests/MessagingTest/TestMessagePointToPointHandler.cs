using System;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Serilog;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;

public class TestMessagePointToPointHandler : IHandlerAsync<TestMessagePointToPoint>
{
    private readonly ILogger _logger;
    private readonly IMessageContextAccessor _messageContextAccessor;
    
    public static string LastUniqueId { get; private set; }
    public static Uri LastQueueUri { get; private set; }

    public TestMessagePointToPointHandler(ILogger logger, IMessageContextAccessor messageContextAccessor)
    {
        _logger = logger;
        _messageContextAccessor = messageContextAccessor;
    }

    public async Task<bool> Handle(TestMessagePointToPoint message)
    {
        LastUniqueId = message.UniqueId;
        LastQueueUri = _messageContextAccessor.MessageContext.QueueUri;

        _logger.Information("Received TestMessage with uniqueId {uniqueId}", message.UniqueId);

        return true;
    }
}