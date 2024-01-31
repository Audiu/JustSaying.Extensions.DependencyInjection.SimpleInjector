using JustSaying.Models;

namespace JustSaying.Extensions.DependencyInjection.SimpleInjector.Tests.MessagingTest;

public class TestMessage : Message
{
    public string UniqueId { get; }

    protected TestMessage()
    {
    }
    
    public TestMessage(string uniqueId)
    {
        UniqueId = uniqueId;
    }
}