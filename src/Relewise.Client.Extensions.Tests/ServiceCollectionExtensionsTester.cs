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

        Assert.That(tracker, Is.Not.Null);
        Assert.That(provider.GetService<IRecommender>(), Is.Not.Null);
        Assert.That(provider.GetService<ISearcher>(), Is.Not.Null);

        Assert.That(datasetId, Is.EqualTo(tracker!.DatasetId));
        Assert.That(tracker.Timeout, Is.EqualTo(TimeSpan.FromSeconds(5)));
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

        ISearcher? searcher = null;
        Assert.DoesNotThrow(() => searcher = provider.GetService<ISearcher>());
        Assert.That(searcher, Is.Not.Null);

        IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
        RelewiseClientOptions options = factory.GetOptions<ISearcher>();

        Assert.That(searcherApiKey, Is.EqualTo(options.ApiKey));
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

        Assert.That(integrationApiKey, Is.EqualTo(options.ApiKey));
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

        Assert.That(tracker, Is.Not.Null);
        Assert.That(tracker!.ServerUrl, Is.EqualTo(serverUrl.ToString()));
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

        Assert.That(tracker, Is.Not.Null);
        Assert.That(defaultServerUrl, Is.EqualTo(tracker!.ServerUrl));
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
        Assert.That(tracker, Is.Not.Null);
        Assert.That(tracker!.Timeout, Is.EqualTo(trackerRequestTimeout));
        Assert.That(provider.GetService<IRecommender>(), Is.Not.Null);
        Assert.That(provider.GetService<ISearcher>(), Is.Not.Null);
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
        Assert.That(tracker.Timeout, Is.EqualTo(timeout));

        var recommender = provider.GetRequiredService<IRecommender>();
        Assert.That(recommender.Timeout, Is.EqualTo(timeout));

        var searcher = provider.GetRequiredService<ISearcher>();
        Assert.That(searcher.Timeout, Is.EqualTo(timeout));
    }
}