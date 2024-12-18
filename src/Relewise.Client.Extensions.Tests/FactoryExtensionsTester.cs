﻿using System;
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

        Assert.That(tracker, Is.Not.Null);
        Assert.That(tracker.Timeout, Is.EqualTo(TimeSpan.FromSeconds(20)));
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
        RelewiseClientOptions globalOptions = new(Guid.NewGuid(), "GlobalApiKey", TimeSpan.FromSeconds(1));
        RelewiseClientOptions recommenderOptions = new(Guid.NewGuid(), "RecommenderApiKey", TimeSpan.FromSeconds(3));
        RelewiseClientOptions searcherOptions = new(Guid.NewGuid(), "SearcherApiKey", TimeSpan.FromSeconds(4));

        IRelewiseClientFactory factory = SetupFactory(options =>
        {
            options.Initialize(globalOptions);
            options.Recommender.Initialize(recommenderOptions);
            options.Searcher.Initialize(searcherOptions);
        });

        Assert.That(globalOptions, Is.EqualTo(factory.GetOptions<ITracker>()));
        Assert.That(recommenderOptions, Is.EqualTo(factory.GetOptions<IRecommender>()));
        Assert.That(searcherOptions, Is.EqualTo(factory.GetOptions<ISearcher>()));
    }


    [Test]
    public void NamedClientOverrides_GlobalOptions()
    {
        RelewiseClientOptions globalOptions = new(Guid.NewGuid(), "GlobalApiKey", TimeSpan.FromSeconds(1));
        RelewiseClientOptions recommenderOptions = new(Guid.NewGuid(), "RecommenderApiKey", TimeSpan.FromSeconds(3));
        RelewiseClientOptions namedRecommenderOptions = new(recommenderOptions.DatasetId, recommenderOptions.ApiKey, TimeSpan.FromSeconds(10));

        IRelewiseClientFactory factory = SetupFactory(options =>
        {
            options.Initialize(globalOptions);
            options.Recommender.Initialize(recommenderOptions);

            options.Named.Add("Integration", integration =>
            {
                integration.Recommender.Initialize(namedRecommenderOptions);
            });
        });

        Assert.That(globalOptions, Is.EqualTo(factory.GetOptions<ITracker>()));
        Assert.That(recommenderOptions, Is.EqualTo(factory.GetOptions<IRecommender>()));
        Assert.That(namedRecommenderOptions, Is.EqualTo(factory.GetOptions<IRecommender>("Integration")));
    }

    [Test]
    public void NamedClientOverrides_DifferentServerUrls()
    {
        RelewiseClientOptions globalOptions = new(Guid.NewGuid(), "GlobalApiKey", TimeSpan.FromSeconds(1));
        RelewiseClientOptions recommenderOptions = new(Guid.NewGuid(), "RecommenderApiKey", TimeSpan.FromSeconds(3));
        RelewiseClientOptions namedRecommenderOptions = new(recommenderOptions.DatasetId, recommenderOptions.ApiKey, TimeSpan.FromSeconds(10), new Uri("https://stage-01.relewise.api"));

        IRelewiseClientFactory factory = SetupFactory(options =>
        {
            options.Initialize(globalOptions);
            options.Recommender.Initialize(recommenderOptions);

            options.Named.Add("Integration", integration =>
            {
                integration.Recommender.Initialize(namedRecommenderOptions);
            });
        });

        Assert.That(globalOptions, Is.EqualTo(factory.GetOptions<ITracker>()));
        Assert.That(recommenderOptions, Is.EqualTo(factory.GetOptions<IRecommender>()));
        Assert.That(namedRecommenderOptions, Is.EqualTo(factory.GetOptions<IRecommender>("Integration")));
        Assert.That(factory.GetOptions<ITracker>().ServerUrl, Is.Not.EqualTo(factory.GetOptions<IRecommender>("Integration").ServerUrl));
    }

    private static IRelewiseClientFactory SetupFactory(Action<RelewiseOptionsBuilder> configure)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddRelewise(configure);

        ServiceProvider provider = serviceCollection.BuildServiceProvider();

        return provider.GetRequiredService<IRelewiseClientFactory>();
    }
}