using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relewise.Client.Extensions.DependencyInjection;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.Tests;

[TestFixture]
public class ServiceCollectionExtenstionTester
{
    [Test]
    public void NullFunction_Exception()
    {
        var serviceCollection = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => serviceCollection.AddRelewise(null!, reset: true));
    }

    [Test]
    public void AddDatasetIdAndApiKey()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = datasetId;
            options.ApiKey = "r4FqfMqtiZjJmoN";
        }, reset: true);

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.IsNotNull(provider.GetService<IRecommender>());
        Assert.IsNotNull(provider.GetService<ISearcher>());

        Assert.AreEqual(datasetId, tracker.DatasetId);
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
        }, reset: true);

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
        }, reset: true);

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetRequiredService<ITracker>();
        Assert.AreEqual(timeout, tracker.Timeout);

        var recommender = provider.GetRequiredService<IRecommender>();
        Assert.AreEqual(timeout, recommender.Timeout);

        var searcher = provider.GetRequiredService<ISearcher>();
        Assert.AreEqual(timeout, searcher.Timeout);
    }
}