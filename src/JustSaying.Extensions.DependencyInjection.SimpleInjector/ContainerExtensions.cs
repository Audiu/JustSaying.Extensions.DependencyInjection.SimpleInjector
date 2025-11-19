using System;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Fluent;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware.Logging;
using JustSaying.Messaging.Middleware.PostProcessing;
using JustSaying.Messaging.Monitoring;
using Newtonsoft.Json;
using SimpleInjector;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector
{

    public static class ContainerExtensions
    {
        public static void AddJustSayingNoOpMessageMonitor(this Container container)
        {
            container.RegisterSingleton<IMessageMonitor, NullOpMessageMonitor>();
        }

        public static MessagingBusBuilder AddJustSayingReturnBuilder(
            this Container container,
            IMessagingConfig messagingConfig,
            Action<MessagingBusBuilder> configure)
        {
            return AddJustSayingReturnBuilder(
                container,
                messagingConfig,
                null,
                configure);
        }

        public static MessagingBusBuilder AddJustSayingReturnBuilder(
            this Container container,
            IMessagingConfig messagingConfig,
            string serviceUrl,
            Action<MessagingBusBuilder> configure)
        {
            var resolver = new ServiceProviderResolver(container);
            container.RegisterInstance(resolver);
            container.RegisterInstance<IHandlerResolver>(resolver);
            container.RegisterInstance<IServiceResolver>(resolver);

            // Register factory lazily to avoid resolving AWS credentials prematurely
            // When using LocalStack (ServiceUrl is set), the builder will configure anonymous credentials
            container.RegisterSingleton<IAwsClientFactory>(() => new DefaultAwsClientFactory());
            container.RegisterSingleton<IAwsClientFactoryProxy>(
                () => new AwsClientFactoryProxy(container.GetInstance<IAwsClientFactory>));

            container.Register<LoggingMiddleware>(Lifestyle.Transient);
            container.Register<SqsPostProcessorMiddleware>(Lifestyle.Transient);
            container.Register<SimpleInjectorScopeMiddleware>(Lifestyle.Transient);

            var messageContextAccessor = new MessageContextAccessor();
            container.RegisterInstance(messageContextAccessor);
            container.RegisterInstance<IMessageContextAccessor>(messageContextAccessor);
            container.RegisterInstance<IMessageContextReader>(messageContextAccessor);

            container.RegisterSingleton<IMessageSubjectProvider, GenericMessageSubjectProvider>();
            container.RegisterSingleton<IVerifyAmazonQueues, AmazonQueueCreator>();

            container.RegisterSingleton<IMessageReceivePauseSignal, MessageReceivePauseSignal>();

            container.RegisterSingleton(() => new JsonSerializerSettings());
            container.RegisterSingleton<IMessageBodySerializationFactory, NewtonsoftSerializationFactory>();

            container.RegisterInstance(messagingConfig);
            container.RegisterInstance(messagingConfig.QueueNamingConvention);
            container.RegisterInstance(messagingConfig.TopicNamingConvention);

            var builder = new MessagingBusBuilder()
                .WithServiceResolver(resolver)
                .Messaging(c => c.WithRegion(messagingConfig.Region))
                .Client(
                    x =>
                    {
                        if (!string.IsNullOrEmpty(serviceUrl))
                        {
                            // The AWS client SDK allows specifying a custom HTTP endpoint.
                            // For testing purposes it is useful to specify a value that
                            // points to a docker image such as `localstack/localstack`
                            x.WithServiceUri(new Uri(serviceUrl)).WithAnonymousCredentials();
                        }
                    });

            configure(builder);

            return builder;
        }
    }
}
