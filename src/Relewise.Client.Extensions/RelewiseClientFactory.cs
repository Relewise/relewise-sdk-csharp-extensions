using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Relewise.Client.Extensions.Infrastructure.Extensions;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions;

internal class RelewiseClientFactory : IRelewiseClientFactory
{
    private readonly IServiceProvider _provider;
    private readonly Dictionary<string, IClient> _clients;
    private readonly Dictionary<string, RelewiseClientOptions> _options;
    private readonly Dictionary<string, RelewiseNamedClientOptions> _namedOptions;

    public RelewiseClientFactory(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _clients = new Dictionary<string, IClient>();
        _options = new Dictionary<string, RelewiseClientOptions>();
        _namedOptions = new Dictionary<string, RelewiseNamedClientOptions>();

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
            RelewiseClientOptions? tracker = AddNamedClient<ITracker, Tracker>(
                name,
                namedClientOptions.Build(trackerOptions),
                namedClientOptions.Tracker,
                (datasetId, apiKey, timeout) => new Tracker(datasetId, apiKey, timeout));

            RelewiseClientOptions? recommender = AddNamedClient<IRecommender, Recommender>(
                name,
                namedClientOptions.Build(recommenderOptions),
                namedClientOptions.Recommender,
                (datasetId, apiKey, timeout) => new Recommender(datasetId, apiKey, timeout));

            RelewiseClientOptions? searcher = AddNamedClient<ISearcher, Searcher>(
                name,
                namedClientOptions.Build(searcherOptions),
                namedClientOptions.Searcher,
                (datasetId, apiKey, timeout) => new Searcher(datasetId, apiKey, timeout));

            _namedOptions.Add(name, new RelewiseNamedClientOptions(
                name,
                namedClientOptions.Build(globalOptions),
                tracker,
                recommender,
                searcher));

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

    private RelewiseClientOptions? AddNamedClient<TInterface, TImplementation>(
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

            return options;
        }

        return default;
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

        if (!_clients.TryGetValue(GenerateClientLookupKey<T>(name), out IClient? namedClient))
            throw new ArgumentException($"No clients with name '{name}' was registered during startup.");

        return (T)namedClient;
    }

    public RelewiseClientOptions GetOptions<T>(string? name = null) where T : class, IClient
    {
        if (!_options.TryGetValue(GenerateClientLookupKey<T>(name), out RelewiseClientOptions? options))
        {
            string exceptionMessage = name == null
                ? "No options has been configured. Please check your call to the 'services.AddRelewise(options => { /* options goes here */ })'-method."
                : $@"No client named '{name}' was registered during startup, thus options cannot be returned. Please check your call to the 'services.AddRelewise(options => {{ /* options goes here */ }})'-method.";

            throw new ArgumentException(exceptionMessage);
        }

        return options;
    }

    public RelewiseNamedClientOptions[] NamedClientOptions => _namedOptions.Values.ToArray();

    private static string GenerateClientLookupKey<T>(string? name = null) => $"{name}_{typeof(T).Name}";

    public delegate void Configure(RelewiseOptionsBuilder builder, IServiceProvider services);

    public IEnumerator<RelewiseNamedClientOptions> GetEnumerator()
    {
        return _namedOptions.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}