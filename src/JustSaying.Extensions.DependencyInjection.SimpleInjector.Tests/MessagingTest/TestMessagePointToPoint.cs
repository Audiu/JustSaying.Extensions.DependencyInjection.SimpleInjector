using JustSaying.Models;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;

public class TestMessagePointToPoint : Message
{
    public string UniqueId { get; }

    protected TestMessagePointToPoint()
    {
    }
    
    public TestMessagePointToPoint(string uniqueId)
    {
        UniqueId = uniqueId;
    }
}