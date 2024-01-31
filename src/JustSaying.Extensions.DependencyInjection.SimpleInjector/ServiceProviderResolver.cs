using System;
using JustSaying.Fluent;
using JustSaying.Messaging.MessageHandling;
using SimpleInjector;
using SimpleInjector.Lifestyles;

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
            // return _container.GetInstance<IHandlerAsync<T>>();

            var scope = new SimpleInjectorScope(_container, AsyncScopedLifestyle.BeginScope(_container));
            return scope.Resolve<IHandlerAsync<T>>();
        }
    }

    internal class SimpleInjectorScope : IDisposable
    {
        private readonly Container _container;
        private readonly Scope _scope;

        public SimpleInjectorScope(Container container, Scope scope)
        {
            _container = container;
            _scope = scope;
        }

        public T Resolve<T>() where T : class
        {
            return _container.GetInstance<T>();
        }

        public void Dispose()
        {
            _scope?.Dispose();
        }
    }
}
