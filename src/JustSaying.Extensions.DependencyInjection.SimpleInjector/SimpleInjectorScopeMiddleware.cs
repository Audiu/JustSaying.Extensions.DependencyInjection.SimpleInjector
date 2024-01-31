using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector
{
    public class SimpleInjectorScopeMiddleware : MiddlewareBase<HandleMessageContext, bool>
    {
        private readonly Container _container;

        public SimpleInjectorScopeMiddleware(Container container)
        {
            _container = container;
        }

        protected override async Task<bool> RunInnerAsync(
            HandleMessageContext context,
            Func<CancellationToken, Task<bool>> func,
            CancellationToken stoppingToken)
        {
            await using (AsyncScopedLifestyle.BeginScope(_container))
            {
                return await func(stoppingToken);
            }
        }
    }
}