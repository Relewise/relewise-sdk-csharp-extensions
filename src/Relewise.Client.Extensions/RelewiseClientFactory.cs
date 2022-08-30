using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Relewise.Client.Extensions.Infrastructure.Extensions;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions;

internal class RelewiseClientFactory : IRelewiseClientFactory
{
    private readonly IServiceProvider _provider;
    private readonly Dictionary<string, IClient> _clients;
    private readonly Dictionary<string, RelewiseClientOptions> _options;

    public RelewiseClientFactory(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _clients = new Dictionary<string, IClient>();
        _options = new Dictionary<string, RelewiseClientOptions>();

        var options = new RelewiseOptionsBuilder();

        foreach (Configure configure in provider.GetServices<Configure>())
            configure(options, provider);

        RelewiseClientOptions? globalOptions;

        try
        {
            globalOptions = options.Build();
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException($"Relewise is missing required configuration. {ex.Message}", ex);
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
            throw new InvalidOperationException($"Options for {typeof(T).Name} is missing required information. {ex.Message}", ex);
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
            throw new InvalidOperationException(
                $"Named client '{name}' for '{typeof(TInterface).Name}' is missing required configuration. {ex.Message}",
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

    public TClient GetClient<TClient>(string? name = null) where TClient : class, IClient
    {
        if (!typeof(TClient).IsInterface) throw new ArgumentException($"Expected generic 'T' to be an interface, e.g. {nameof(ITracker)}.");

        if (name == null)
        {
            TClient? client = _provider.GetService<TClient>();

            if (client == null)
                throw new ArgumentException($"No client for {typeof(TClient).Name} was registered during startup");

            return client;
        }

        if (!_clients.TryGetValue(GenerateClientLookupKey<TClient>(name), out IClient? namedClient))
            throw new ArgumentException($"No clients with name '{name}' was registered during startup.");

        return (TClient) namedClient;
    }

    public RelewiseClientOptions GetOptions<TClient>(string? name = null) where TClient : class, IClient
    {
        if (!_options.TryGetValue(GenerateClientLookupKey<TClient>(name), out RelewiseClientOptions? options))
        {
            string exceptionMessage = name == null
                ? "No options has been configured. Please check your call to the 'services.AddRelewise(options => { /* options goes here */ })'-method."
                : $@"No client named '{name}' was registered during startup, thus options cannot be returned. Please check your call to the 'services.AddRelewise(options => {{ /* options goes here */ }})'-method.";

            throw new ArgumentException(exceptionMessage);
        }

        return options;
    }

    public bool Contains<TClient>(string? name = null) where TClient : class, IClient
    {
        return _options.ContainsKey(GenerateClientLookupKey<TClient>(name));
    }

    private static string GenerateClientLookupKey<T>(string? name = null) => $"{name}_{typeof(T).Name}";

    public delegate void Configure(RelewiseOptionsBuilder builder, IServiceProvider services);
}