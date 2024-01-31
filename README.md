# JustSaying.Extensions.DependencyInjection.SimpleInjector

This package provides DI configuration for JustSaying (v7 currently) using SimpleInjector. Additionally it provides a middleware which can be used to resolve dependencies within a SimpleInjector async scope.

Currently this is a work in progress and is not yet published to NuGet.

## How to use

```csharp
var builder = container.AddJustSayingReturnBuilder(
    new AwsConfig(null, null, "eu-west-1", "http://localhost.localstack.cloud:4566"),
    namingStrategy,
    namingStrategy,
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

container.Register<IHandlerAsync<TestMessage>, TestMessageHandler>(Lifestyle.Scoped);

container.RegisterSingleton(() => builder.BuildPublisher());
container.RegisterSingleton(() => builder.BuildSubscribers());
```

## Additional Info

The SimpleInjector scope middleware should always be first, then additional middlewares can be added after, and when
they are resolved they will be resolved within the scope.

This can allow extra middleware to be added for UoW DB transactions etc.