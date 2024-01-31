using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using SimpleInjector;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector
{
    public class ServiceProviderResolver : IServiceResolver, IHandlerResolver
    {
        private readonly Container _container;

        public ServiceProviderResolver(Container container)
        {
            _container = container;
        }

        public T ResolveService<T>() where T : class
        {
            return _container.GetInstance<T>();
        }

        public T ResolveOptionalService<T>() where T : class
        {
            return _container.GetInstance<T>();
        }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            return _container.GetInstance<IHandlerAsync<T>>();
        }
    }
}