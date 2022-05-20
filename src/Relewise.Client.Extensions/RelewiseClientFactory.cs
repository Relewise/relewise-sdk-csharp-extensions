using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Relewise.Client.Extensions.Infrastructure.Extensions;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions;

internal class RelewiseClientFactory : IRelewiseClientFactory
{
    private readonly Dictionary<string, IClient> _clients;
    private readonly Dictionary<string, RelewiseClientOptions> _options;
    private readonly IServiceProvider _provider;

    public RelewiseClientFactory(RelewiseOptionsBuilder options, IServiceProvider provider)
    {
        _clients = new Dictionary<string, IClient>();
        _options = new Dictionary<string, RelewiseClientOptions>();
        _provider = provider;

        RelewiseClientOptions? globalOptions;

        try
        {
            globalOptions = options.Build();
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Relewise is missing required configuration. {ex.Message}", ex);
        }

        RelewiseClientOptions? trackerOptions = AddOptions<ITracker>(globalOptions, options.Tracker);
        RelewiseClientOptions? recommenderOptions = AddOptions<IRecommender>(globalOptions, options.Recommender);
        RelewiseClientOptions? searcherOptions = AddOptions<ISearcher>(globalOptions, options.Searcher);

        foreach ((string name, RelewiseClientsOptionsBuilder namedClientOptions) in options.Named.Clients.AsTuples())
        {
            AddNamedClient<ITracker, Tracker>(
                name,
                namedClientOptions.Build(trackerOptions),
                namedClientOptions.Tracker,
                (datasetId, apiKey, timeout) => new Tracker(datasetId, apiKey, timeout));

            AddNamedClient<IRecommender, Recommender>(
                name,
                namedClientOptions.Build(recommenderOptions),
                namedClientOptions.Recommender,
                (datasetId, apiKey, timeout) => new Recommender(datasetId, apiKey, timeout));

            AddNamedClient<ISearcher, Searcher>(
                name,
                namedClientOptions.Build(searcherOptions),
                namedClientOptions.Searcher,
                (datasetId, apiKey, timeout) => new Searcher(datasetId, apiKey, timeout));
        }
    }

    private RelewiseClientOptions? AddOptions<T>(
        RelewiseClientOptions? globalOptions, 
        RelewiseClientOptionsBuilder clientOptions)
    {
        RelewiseClientOptions? options;

        try
        {
            options = clientOptions.Build(globalOptions);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex);
            throw;
        }

        if (options != null)
            _options.Add(GenerateClientLookupKey<T>(), options);

        return options;
    }

    private void AddNamedClient<TInterface, TImplementation>(
        string name,
        RelewiseClientOptions? globalClientOptions,
        RelewiseClientOptionsBuilder namedClientOptions,
        Func<Guid, string, TimeSpan, TImplementation> create)
        where TInterface : class, IClient
        where TImplementation : TInterface
    {
        RelewiseClientOptions? options;

        try
        {
            options = namedClientOptions.Build(globalClientOptions);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(
                $"Configuring named client '{name}' for '{typeof(TInterface).Name}' resulted in an error: {ex.Message}",
                ex);
        }

        if (options != null)
        {
            TImplementation client = create(
                options.DatasetId,
                options.ApiKey,
                options.Timeout);

            _clients.Add(GenerateClientLookupKey<TInterface>(name), client);
            _options.Add(GenerateClientLookupKey<TInterface>(name), options);
        }
    }

    public T GetClient<T>(string? name = null) where T : class, IClient
    {
        if (!typeof(T).IsInterface) throw new ArgumentException($"Expected generic 'T' to be an interface, e.g. {nameof(ITracker)}.");

        if (name == null)
        {
            T? client = _provider.GetService<T>();

            if (client == null)
                throw new ArgumentException($"No client for {typeof(T).Name} was registered during startup");

            return client;
        }

        if (!_clients.TryGetValue(GenerateClientLookupKey<T>(name), out IClient namedClient))
            throw new ArgumentException($"No clients with name '{name}' was registered during startup.");

        return (T) namedClient;
    }

    public RelewiseClientOptions GetOptions<T>(string? name = null) where T : class, IClient
    {
        if (!_options.TryGetValue(GenerateClientLookupKey<T>(name), out RelewiseClientOptions options))
        {
            string exceptionMessage = name == null
                ? "No default clients (clients without a name) was registered during startup, thus options cannot be returned."
                : "No client named '{name}' was registered during startup, thus options cannot be returned.";

            throw new ArgumentException(exceptionMessage);
        }

        return options;
    }

    private static string GenerateClientLookupKey<T>(string? name = null) => $"{name}_{typeof(T).Name}";
}