using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relewise.Client.Extensions.DependencyInjection;

namespace Relewise.Client.Extensions.Tests;

[TestFixture]
public class FactoryExtensionsTester
{
    [Test]
    public void FactoryExample_Found()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(options =>
        {
            options.Named.Add("Integration", integration =>
            {
                integration.DatasetId = Guid.NewGuid();
                integration.ApiKey = "r4FqfMqtiZjJmoN";
                integration.Tracker.Timeout = TimeSpan.FromSeconds(20);
            });
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        ITracker tracker = factory.GetClient<ITracker>("Integration");

        Assert.IsNotNull(tracker);
        Assert.AreEqual(TimeSpan.FromSeconds(20), tracker.Timeout);
    }

    [Test]
    public void FactoryExample_Missing()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";

            options.Named.Add("Integration", _ => { });
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        Assert.Catch<ArgumentException>(() => factory.GetClient<ITracker>("Int"));
    }

    [Test]
    public void FactoryExample_UsingWrongType()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";

            options.Named.Add("Integration", _ => { });
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        Assert.Catch<ArgumentException>(() => factory.GetClient<Tracker>("Integration"));
    }
}