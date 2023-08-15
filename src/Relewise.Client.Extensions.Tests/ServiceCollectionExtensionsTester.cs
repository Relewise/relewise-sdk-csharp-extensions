using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relewise.Client.Extensions.DependencyInjection;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTester
{
    [Test]
    public void AddDatasetIdAndApiKey()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = datasetId;
            options.ApiKey = "r4FqfMqtiZjJmoN";
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.IsNotNull(provider.GetService<IRecommender>());
        Assert.IsNotNull(provider.GetService<ISearcher>());

        Assert.AreEqual(datasetId, tracker.DatasetId);
        Assert.AreEqual(TimeSpan.FromSeconds(5), tracker.Timeout);
    }

    [Test]
    public void AddDatasetServerUrl()
    {
        var serviceCollection = new ServiceCollection();

        const string serverUrl = "test value";

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";
            options.ServerUrl = serverUrl;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.IsNotNull(provider.GetService<IRecommender>());
        Assert.IsNotNull(provider.GetService<ISearcher>());

        Assert.AreEqual(serverUrl, tracker.ServerUrl);
        Assert.AreEqual(TimeSpan.FromSeconds(5), tracker.Timeout);
    }

    [Test]
    public void NotAddServerUrl()
    {
        var serviceCollection = new ServiceCollection();

        const string baseProductionServerUrl = "https://api.relewise.com";

        serviceCollection.AddRelewise(options =>
            {
                options.DatasetId = Guid.NewGuid();
                options.ApiKey = "r4FqfMqtiZjJmoN";
            });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.IsNotNull(provider.GetService<IRecommender>());
        Assert.IsNotNull(provider.GetService<ISearcher>());

        Assert.AreEqual(baseProductionServerUrl, tracker.ServerUrl);
        Assert.AreEqual(TimeSpan.FromSeconds(5), tracker.Timeout);
    }

    [Test]
    public void SetSpecificTimeOuts()
    {
        var serviceCollection = new ServiceCollection();
        var trackerRequestTimeout = TimeSpan.FromSeconds(20);
        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";
            options.Tracker.Timeout = trackerRequestTimeout;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();
        Assert.IsNotNull(tracker);
        Assert.AreEqual(trackerRequestTimeout, tracker.Timeout);
        Assert.IsNotNull(provider.GetService<IRecommender>());
        Assert.IsNotNull(provider.GetService<ISearcher>());
    }

    [Test]
    public void SetGlobalTimeOuts()
    {
        var serviceCollection = new ServiceCollection();
        var timeout = TimeSpan.FromSeconds(30);
        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";
            options.Timeout = timeout;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetRequiredService<ITracker>();
        Assert.AreEqual(timeout, tracker.Timeout);

        var recommender = provider.GetRequiredService<IRecommender>();
        Assert.AreEqual(timeout, recommender.Timeout);

        var searcher = provider.GetRequiredService<ISearcher>();
        Assert.AreEqual(timeout, searcher.Timeout);
    }
}