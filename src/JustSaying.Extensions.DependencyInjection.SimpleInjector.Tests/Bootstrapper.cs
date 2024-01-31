using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests;

[SetUpFixture]
public class Bootstrapper
{
    public static ILoggerFactory LoggerFactory { get; private set; }

    public static Container Container { get; private set; }

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        LoggerFactory = new LoggerFactory();

        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        LoggerFactory.AddSerilog(logger);

        Log.Logger = logger;

        logger.Information("Configured logging");
        TestContext.Progress.WriteLine("Configured logging");

        try
        {
            Container = new Container();
            ConfigureInjection(Container);

            Container.Verify();

            logger.Information("Configured and verified runtime injection");
            TestContext.Progress.WriteLine("Configured and verified runtime injection");

            // Boot listener
            var publisher = Container.GetInstance<IMessagePublisher>();
            await publisher.StartAsync(CancellationToken.None);

            var messagingBus = Container.GetInstance<IMessagingBus>();
            await messagingBus.StartAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to bootstrap");
            TestContext.Progress.WriteLine($"Failed to bootstrap {ex.Message}");

            throw;
        }
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        Container?.Dispose();
        LoggerFactory?.Dispose();
    }

    private static void ConfigureInjection(Container container)
    {
        container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

        container.RegisterInstance(Log.Logger);

        ConfigureJustSaying(container);
    }

    private static void ConfigureJustSaying(Container container)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog(Log.Logger);

        container.RegisterInstance<ILoggerFactory>(loggerFactory);

        var builder = container.AddJustSayingReturnBuilder(
            new AwsConfig(null, null, "eu-west-1", "http://localhost.localstack.cloud:4566"),
            null,
            null,
            builder =>
            {
                builder.Subscriptions(
                    x =>
                    {
                        x.ForTopic<TestMessage>(
                            cfg =>
                            {
                                cfg.WithMiddlewareConfiguration(m => { m.UseSimpleInjectorScope(); });
                            });

                        x.ForQueue<TestMessagePointToPoint>(
                            cfg =>
                            {
                                cfg.WithMiddlewareConfiguration(m => { m.UseSimpleInjectorScope(); });
                            });
                    }
                );

                builder.Publications(
                    x =>
                    {
                        x.WithTopic<TestMessage>();
                        x.WithQueue<TestMessagePointToPoint>();
                    });
            });

        container.Register<IHandlerAsync<TestMessage>, TestMessageHandler>(Lifestyle.Scoped);
        container.Register<IHandlerAsync<TestMessagePointToPoint>, TestMessagePointToPointHandler>(Lifestyle.Scoped);

        // Final steps (we might want to override our publishers/subscribers)
        container.RegisterSingleton(() => builder.BuildPublisher());
        container.RegisterSingleton(() => builder.BuildSubscribers());
    }
}
