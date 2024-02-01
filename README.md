# JustSaying.Extensions.DependencyInjection.SimpleInjector

This package provides DI configuration for [JustSaying](https://github.com/justeattakeaway/JustSaying) v7 using SimpleInjector. 
Additionally it provides a middleware which can be used to resolve dependencies within a SimpleInjector async scope.

## How to use

```csharp
// Register all the bits JustSaying needs as its core, and return the builder
var builder = container.AddJustSayingReturnBuilder(
    new AwsConfig(null, null, "eu-west-1", "http://localhost.localstack.cloud:4566"),
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