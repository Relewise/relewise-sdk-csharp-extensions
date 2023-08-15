using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relewise.Client.Extensions.DependencyInjection;
using Relewise.Client.Search;

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

    [Test]
    public void ClientOverrides_GlobalOptions()
    {
        RelewiseClientOptions globalOptions = new (Guid.NewGuid(), "GlobalApiKey", TimeSpan.FromSeconds(1), "https://stage01-api.relewise.com");
        RelewiseClientOptions recommenderOptions = new(Guid.NewGuid(), "RecommenderApiKey", TimeSpan.FromSeconds(3), "https://stage01-api.relewise.com");
        RelewiseClientOptions searcherOptions = new(Guid.NewGuid(), "SearcherApiKey", TimeSpan.FromSeconds(4), "https://stage01-api.relewise.com");

        IRelewiseClientFactory factory = SetupFactory(options =>
        {
            options.Initialize(globalOptions);
            options.Recommender.Initialize(recommenderOptions);
            options.Searcher.Initialize(searcherOptions);
        });

        Assert.AreEqual(globalOptions, factory.GetOptions<ITracker>());
        Assert.AreEqual(recommenderOptions, factory.GetOptions<IRecommender>());
        Assert.AreEqual(searcherOptions, factory.GetOptions<ISearcher>());
    }


    [Test]
    public void NamedClientOverrides_GlobalOptions()
    {
        RelewiseClientOptions globalOptions = new(Guid.NewGuid(), "GlobalApiKey", TimeSpan.FromSeconds(1), "https://stage01-api.relewise.com");
        RelewiseClientOptions recommenderOptions = new(Guid.NewGuid(), "RecommenderApiKey", TimeSpan.FromSeconds(3), "https://stage01-api.relewise.com");
        RelewiseClientOptions namedRecommenderOptions = new(recommenderOptions.DatasetId, recommenderOptions.ApiKey, TimeSpan.FromSeconds(10), "https://stage01-api.relewise.com");

        IRelewiseClientFactory factory = SetupFactory(options =>
        {
            options.Initialize(globalOptions);
            options.Recommender.Initialize(recommenderOptions);
            
            options.Named.Add("Integration", integration =>
            {
                integration.Recommender.Initialize(namedRecommenderOptions);
            });
        });

        Assert.AreEqual(globalOptions, factory.GetOptions<ITracker>());
        Assert.AreEqual(recommenderOptions, factory.GetOptions<IRecommender>());
        Assert.AreEqual(namedRecommenderOptions, factory.GetOptions<IRecommender>("Integration"));
    }

    private static IRelewiseClientFactory SetupFactory(Action<RelewiseOptionsBuilder> configure)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(configure);

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        return provider.GetRequiredService<IRelewiseClientFactory>();
    }
}