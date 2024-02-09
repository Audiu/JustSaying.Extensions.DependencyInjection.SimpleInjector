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
            AwsConfig awsConfig,
            Action<MessagingBusBuilder> configure)
        {
            var messagingConfig = new MessagingConfig
            {
                Region = awsConfig.RegionEndpoint,
            };

            return AddJustSayingReturnBuilder(
                container,
                awsConfig,
                messagingConfig,
                configure);
        }

        public static MessagingBusBuilder AddJustSayingReturnBuilder(
            this Container container,
            AwsConfig awsConfig,
            IMessagingConfig messagingConfig,
            Action<MessagingBusBuilder> configure)
        {
            container.RegisterInstance(awsConfig);

            var resolver = new ServiceProviderResolver(container);
            container.RegisterInstance(resolver);
            container.RegisterInstance<IHandlerResolver>(resolver);
            container.RegisterInstance<IServiceResolver>(resolver);

            container.RegisterInstance<IAwsClientFactory>(new DefaultAwsClientFactory());
            container.RegisterSingleton<IAwsClientFactoryProxy>(
                () => new AwsClientFactoryProxy(container.GetInstance<IAwsClientFactory>));

            container.Register<LoggingMiddleware>(Lifestyle.Transient);
            container.Register<SqsPostProcessorMiddleware>(Lifestyle.Transient);
            container.Register<SimpleInjectorScopeMiddleware>(Lifestyle.Transient);

            var messageContextAccessor = new MessageContextAccessor();
            container.RegisterInstance(messageContextAccessor);
            container.RegisterInstance<IMessageContextAccessor>(messageContextAccessor);
            container.RegisterInstance<IMessageContextReader>(messageContextAccessor);

            var messageSerializationFactory = new NewtonsoftSerializationFactory();
            container.RegisterInstance<IMessageSerializationFactory>(messageSerializationFactory);
            container.RegisterSingleton<IMessageSubjectProvider, GenericMessageSubjectProvider>();
            container.RegisterSingleton<IVerifyAmazonQueues, AmazonQueueCreator>();

            container.RegisterInstance<IMessageSerializationRegister>(
                new MessageSerializationRegister(
                    messagingConfig.MessageSubjectProvider,
                    messageSerializationFactory));

            container.RegisterSingleton<IMessageReceivePauseSignal, MessageReceivePauseSignal>();

            container.RegisterInstance(messagingConfig);
            container.RegisterInstance(messagingConfig.QueueNamingConvention);
            container.RegisterInstance(messagingConfig.TopicNamingConvention);

            var builder = new MessagingBusBuilder()
                .WithServiceResolver(resolver)
                .Client(
                    x =>
                    {
                        if (!string.IsNullOrEmpty(awsConfig.ServiceUrl))
                        {
                            // The AWS client SDK allows specifying a custom HTTP endpoint.
                            // For testing purposes it is useful to specify a value that
                            // points to a docker image such as `localstack/localstack`
                            x.WithServiceUri(new Uri(awsConfig.ServiceUrl)).WithAnonymousCredentials();
                        }
                        else
                        {
                            // The real AWS environment will require some means of authentication
                            x.WithBasicCredentials(awsConfig.AccessKey, awsConfig.SecretKey);
                            //x.WithSessionCredentials("###", "###", "###");
                        }
                    });

            configure(builder);

            return builder;
        }
    }
}
