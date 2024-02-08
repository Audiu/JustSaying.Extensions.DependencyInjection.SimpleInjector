using System;
using System.Threading.Tasks;
using FluentAssertions;
using JustSaying.Messaging;
using Newtonsoft.Json;
using NUnit.Framework;
using Serilog;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;

[TestFixture]
public class MessagingTests
{
    [Test]
    public async Task Interrogate()
    {
        Log.Information(
            $"Publisher: {JsonConvert.SerializeObject(Bootstrapper.Container.GetInstance<IMessagePublisher>().Interrogate())}");

        Log.Information(
            $"Bus: {JsonConvert.SerializeObject(Bootstrapper.Container.GetInstance<IMessagingBus>().Interrogate())}");

        JsonConvert.SerializeObject(Bootstrapper.Container.GetInstance<IMessagePublisher>().Interrogate()).Should().Contain("eu-west-1");
        JsonConvert.SerializeObject(Bootstrapper.Container.GetInstance<IMessagingBus>().Interrogate()).Should().Contain("eu-west-1");
    }

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

    [Test]
    public async Task TestHandlerShouldBeNewInstances()
    {
        TestMessageHandler.InstanceIds.Clear();

        var messagePublisher = Bootstrapper.Container.GetInstance<IMessagePublisher>();

        await messagePublisher.PublishAsync(new TestMessage(Guid.NewGuid().ToString()));
        await messagePublisher.PublishAsync(new TestMessage(Guid.NewGuid().ToString()));

        await Task.Delay(2000);

        TestMessageHandler.InstanceIds.Should().HaveCount(2);
        TestMessageHandler.InstanceIds.Should().OnlyHaveUniqueItems();
    }
}
