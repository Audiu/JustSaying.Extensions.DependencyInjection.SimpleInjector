# JustSaying.Extensions.DependencyInjection.SimpleInjector

This package provides DI configuration for [JustSaying](https://github.com/justeattakeaway/JustSaying) v8 using SimpleInjector. 
Additionally, it provides a middleware which can be used to resolve dependencies within a SimpleInjector async scope.

AWS credentials are loaded in the normal AWS expected manner of loading, env vars, profile, EC2 instance bits.

A region is supplied via the `IMessagingConfig` which is passed in. In addition, naming conventions and compression
options can be passed in via here.

NOTE: If no messaging stats are required, then `container.AddJustSayingNoOpMessageMonitor();` must be called.

If you want to provide a `serviceUrl` (for example for testing with localstack), provide this using the overload
form of `AddJustSayingReturnBuilder()` as the 2nd param.

Checkout the integration tests (which use TestContainers) as an example.

## How to use

```csharp
container.AddJustSayingNoOpMessageMonitor();

// Register all the bits JustSaying needs as its core, and return the builder
var builder = container.AddJustSayingReturnBuilder(
    new MessagingConfig 
    {
        Region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-1",
    }
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
            }
        );

        builder.Publications(
            x =>
            {
                x.WithTopic<TestMessage>();
            });
    });

// Register our handlers
container.Register<IHandlerAsync<TestMessage>, TestMessageHandler>(Lifestyle.Scoped);
// ...

// Finally builder the publisher/subscriber pipelines and register IMessagePublisher and IMessageBus to the container
// This is done outside the AddJustSaying extension as we might want to replace/extend these, or not build both.
container.RegisterSingleton(() => builder.BuildPublisher());
container.RegisterSingleton(() => builder.BuildSubscribers());
```

## Additional Info

The SimpleInjector scope middleware should always be first, then additional middlewares can be added after, and when
they are resolved they will be resolved within the scope.

This can allow extra middleware to be added for UoW DB transactions etc.
