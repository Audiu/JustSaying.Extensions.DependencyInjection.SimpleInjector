using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Serilog;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;

public class TestMessageHandler : IHandlerAsync<TestMessage>
{
    private readonly ILogger _logger;
    private readonly IMessageContextAccessor _messageContextAccessor;

    public static string LastUniqueId { get; private set; }
    public static Uri LastQueueUri { get; private set; }

    private readonly string _instanceId = Guid.NewGuid().ToString();
    public static IList<string> InstanceIds { get; } = new List<string>();

    public TestMessageHandler(ILogger logger, IMessageContextAccessor messageContextAccessor)
    {
        _logger = logger;
        _messageContextAccessor = messageContextAccessor;
    }

    public async Task<bool> Handle(TestMessage message)
    {
        LastUniqueId = message.UniqueId;
        LastQueueUri = _messageContextAccessor.MessageContext.QueueUri;

        InstanceIds.Add(_instanceId);

        _logger.Information("Received TestMessage with uniqueId {uniqueId}", message.UniqueId);

        return true;
    }
}
