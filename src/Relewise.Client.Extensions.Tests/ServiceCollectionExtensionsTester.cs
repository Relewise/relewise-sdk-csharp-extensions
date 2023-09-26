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

        var serverUrl = new Uri("https://valid-absolute-uri.com/");

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";
            options.ServerUrl = serverUrl;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.AreEqual(tracker.ServerUrl, serverUrl.ToString());
    }

    [Test]
    public void NotAllowInvalidDatasetServerUrl()
    {
        var serviceCollection = new ServiceCollection();

        var serverUrl = new Uri("invalidabsoluteuri", UriKind.RelativeOrAbsolute);

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = Guid.NewGuid();
            options.ApiKey = "r4FqfMqtiZjJmoN";
            options.ServerUrl = serverUrl;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();
        Assert.Catch<ArgumentException>(() => provider.GetService<ITracker>());
    }

    [Test]
    public void NotAddServerUrl()
    {
        var serviceCollection = new ServiceCollection();

        const string defaultServerUrl = "https://api.relewise.com";

        serviceCollection.AddRelewise(options =>
            {
                options.DatasetId = Guid.NewGuid();
                options.ApiKey = "r4FqfMqtiZjJmoN";
            });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.AreEqual(defaultServerUrl, tracker.ServerUrl);
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

    [Test]
    public void OnlySetOnClient()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();

        serviceCollection.AddRelewise(options =>
        {
            options.Tracker.DatasetId = datasetId;
            options.Tracker.ApiKey = "r4FqfMqtiZjJmoN";
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        var tracker = provider.GetService<ITracker>();

        Assert.IsNotNull(tracker);
        Assert.AreEqual(datasetId, tracker.DatasetId);
    }

    [Test]
    public void OnlySetOnClientWithoutApiKey()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();

        serviceCollection.AddRelewise(options =>
        {
            options.Tracker.DatasetId = datasetId;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        Assert.Throws<ArgumentException>(() => provider.GetService<ITracker>());
    }

    [Test]
    public void OnlySetOnWrongClient()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();

        serviceCollection.AddRelewise(options =>
        {
            options.Searcher.DatasetId = datasetId;
            options.Searcher.ApiKey = "r4FqfMqtiZjJmoN";
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetService<ITracker>());
    }

    [Test]
    public void ThrowException_WhenNoDatasetId()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(options =>
        {
            options.Tracker.ApiKey = "r4FqfMqtiZjJmoN";
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();
        Assert.Throws<InvalidOperationException>(() => provider.GetService<ITracker>());
    }

    [Test]
    public void ThrowException_WhenNoApiKey()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(options =>
        {
            options.Tracker.DatasetId = Guid.NewGuid();
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();
        Assert.Throws<ArgumentException>(() => provider.GetService<ITracker>());
    }


    [Test]
    public void test()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();
        var searcherApiKey = "r4FqfMqtiZjJmoN";

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = datasetId;
            options.ApiKey = searcherApiKey;
            options.Searcher.Timeout = new TimeSpan(0, 0, 10);
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        ISearcher searcher = null;
        Assert.DoesNotThrow(() => searcher = provider.GetService<ISearcher>());
        Assert.IsNotNull(searcher);

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        RelewiseClientOptions options = factory.GetOptions<ISearcher>();

        Assert.AreEqual(searcherApiKey, options.ApiKey);
    }

    [Test]
    public void SetDatasetIdGloballyButApiKeyOnlyOnSpecificClient()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();
        var searcherApiKey = "r4FqfMqtiZjJmoN";

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = datasetId;
            options.Searcher.ApiKey = searcherApiKey;
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        ISearcher searcher = null;
        Assert.DoesNotThrow(() => searcher = provider.GetService<ISearcher>());
        Assert.IsNotNull(searcher);

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        RelewiseClientOptions options = factory.GetOptions<ISearcher>();

        Assert.AreEqual(searcherApiKey, options.ApiKey);
    }

    [Test]
    public void SetDatasetIdGloballyButApiKeyOnlyOnNamedClient()
    {
        var serviceCollection = new ServiceCollection();

        var datasetId = Guid.NewGuid();
        var integrationApiKey = "r4FqfMqtiZjJmoN";

        serviceCollection.AddRelewise(options =>
        {
            options.DatasetId = datasetId;
            options.Named.Add("Integration", integration => integration.ApiKey = integrationApiKey);
        });

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        RelewiseClientOptions options = factory.GetOptions<ITracker>("Integration");

        Assert.AreEqual(integrationApiKey, options.ApiKey);
    }
}