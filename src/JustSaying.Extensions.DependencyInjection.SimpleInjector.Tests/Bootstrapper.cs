using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
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
    private static IContainer _localStackContainer;

    public static ILoggerFactory LoggerFactory { get; private set; }

    public static Container Container { get; private set; }

    public static string LocalStackServiceUrl { get; private set; }

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
            // Start LocalStack container
            logger.Information("Starting LocalStack container...");
            TestContext.Progress.WriteLine("Starting LocalStack container...");

            _localStackContainer = new ContainerBuilder()
                .WithImage("localstack/localstack:latest")
                .WithPortBinding(4566, true)
                .WithEnvironment("SERVICES", "sns,sqs")
                .WithEnvironment("DEBUG", "0")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(4566))
                .Build();

            await _localStackContainer.StartAsync();

            var port = _localStackContainer.GetMappedPublicPort(4566);
            LocalStackServiceUrl = $"http://{_localStackContainer.Hostname}:{port}";

            logger.Information($"LocalStack started at {LocalStackServiceUrl}");
            TestContext.Progress.WriteLine($"LocalStack started at {LocalStackServiceUrl}");

            Container = new Container();
            ConfigureInjection(Container);

            // Set dummy AWS credentials to allow Container.Verify() to succeed
            // These won't be used because LocalStack is configured with anonymous credentials
            var originalAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var originalSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

            try
            {
                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");

                Container.Verify();
            }
            finally
            {
                // Restore original values
                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", originalAccessKey);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", originalSecretKey);
            }

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
    public async Task FixtureTearDown()
    {
        Container?.Dispose();
        LoggerFactory?.Dispose();

        if (_localStackContainer != null)
        {
            await _localStackContainer.DisposeAsync();
        }
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

        container.AddJustSayingNoOpMessageMonitor();

        var builder = container.AddJustSayingReturnBuilder(
            new MessagingConfig
            {
                Region = "eu-west-1",
            },
            LocalStackServiceUrl,
            builder =>
            {
                builder.Subscriptions(
                    x =>
                    {
                        x.ForTopic<TestMessage>(
                            cfg =>
                            {
                                cfg.WithMiddlewareConfiguration(m =>
                                {
                                    m.UseSimpleInjectorScope();
                                    m.UseDefaults<TestMessage>(typeof(TestMessageHandler)); // Add default middleware pipeline
                                });
                            });

                        x.ForQueue<TestMessagePointToPoint>(
                            cfg =>
                            {
                                cfg.WithMiddlewareConfiguration(m =>
                                {
                                    m.UseSimpleInjectorScope();
                                    m.UseDefaults<TestMessagePointToPoint>(typeof(TestMessagePointToPointHandler)); // Add default middleware pipeline
                                });
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
