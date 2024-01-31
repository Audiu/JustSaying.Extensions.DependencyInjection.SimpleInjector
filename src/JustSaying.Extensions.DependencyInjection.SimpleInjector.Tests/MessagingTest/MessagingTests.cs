using System;
using System.Threading.Tasks;
using FluentAssertions;
using JustSaying.Messaging;
using NUnit.Framework;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;

[TestFixture]
public class MessagingTests
{
    [Test]
    public async Task TestTopic()
    {
        var id = Guid.NewGuid().ToString();

        var messagePublisher = Bootstrapper.Container.GetInstance<IMessagePublisher>();

        await messagePublisher.PublishAsync(new TestMessage(id));

        await Task.Delay(2000);

        TestMessageHandler.LastUniqueId.Should().Be(id);
        TestMessageHandler.LastQueueUri.ToString().Should().Contain("testmessage");
    }

    [Test]
    public async Task TestQueue()
    {
        var id = Guid.NewGuid().ToString();

        var messagePublisher = Bootstrapper.Container.GetInstance<IMessagePublisher>();

        await messagePublisher.PublishAsync(new TestMessagePointToPoint(id));

        await Task.Delay(2000);

        TestMessagePointToPointHandler.LastUniqueId.Should().Be(id);
        TestMessagePointToPointHandler.LastQueueUri.ToString().Should().Contain("testmessagepointtopoint");
    }
}
