using JustSaying.Extensions.DependencyInjection.SimpleInjector;

// ReSharper disable once CheckNamespace
namespace JustSaying.Messaging.Middleware
{
    public static class MiddlewareExtensions
    {
        public static HandlerMiddlewareBuilder UseSimpleInjectorScope(this HandlerMiddlewareBuilder builder)
        {
            builder.Use<SimpleInjectorScopeMiddleware>();

            return builder;
        }
    }
}