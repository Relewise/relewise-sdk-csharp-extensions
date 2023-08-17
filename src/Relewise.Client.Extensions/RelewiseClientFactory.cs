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
    private readonly List<string> _clientNames;

    public RelewiseClientFactory(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _clients = new Dictionary<string, IClient>();
        _options = new Dictionary<string, RelewiseClientOptions>();
        _clientNames = new List<string>();

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
        RelewiseClientOptions? searchAdministratorOptions = AddOptions<ISearchAdministrator>(globalOptions, options.SearchAdministrator);
        RelewiseClientOptions? analyzerOptions = AddOptions<IAnalyzer>(globalOptions, options.Analyzer);
        RelewiseClientOptions? dataAccessorOptions = AddOptions<IDataAccessor>(globalOptions, options.DataAccessor);

        foreach ((string name, RelewiseClientsOptionsBuilder namedClientOptions) in options.Named.Clients.AsTuples())
        {
            _clientNames.Add(name);

            AddNamedClient<ITracker, Tracker>(
                name,
                namedClientOptions.Build(trackerOptions),
                namedClientOptions.Tracker,
                (datasetId, apiKey, timeout, serverUrl) => new Tracker(datasetId, apiKey, timeout).ConfigureClient(serverUrl));

            AddNamedClient<IRecommender, Recommender>(
                name,
                namedClientOptions.Build(recommenderOptions),
                namedClientOptions.Recommender,
                (datasetId, apiKey, timeout, serverUrl) => new Recommender(datasetId, apiKey, timeout).ConfigureClient(serverUrl));

            AddNamedClient<ISearcher, Searcher>(
                name,
                namedClientOptions.Build(searcherOptions),
                namedClientOptions.Searcher,
                (datasetId, apiKey, timeout, serverUrl) => new Searcher(datasetId, apiKey, timeout).ConfigureClient(serverUrl));

            AddNamedClient<IDataAccessor, DataAccessor>(
                name,
                namedClientOptions.Build(dataAccessorOptions),
                namedClientOptions.DataAccessor,
                (datasetId, apiKey, timeout, serverUrl) => new DataAccessor(datasetId, apiKey, timeout).ConfigureClient(serverUrl));

            AddNamedClient<ISearchAdministrator, SearchAdministrator>(
                name,
                namedClientOptions.Build(searchAdministratorOptions),
                namedClientOptions.SearchAdministrator,
                (datasetId, apiKey, timeout, serverUrl) => new SearchAdministrator(datasetId, apiKey, timeout).ConfigureClient(serverUrl));

            AddNamedClient<IAnalyzer, Analyzer>(
                name,
                namedClientOptions.Build(analyzerOptions),
                namedClientOptions.Analyzer,
                (datasetId, apiKey, timeout, serverUrl) => new Analyzer(datasetId, apiKey, timeout).ConfigureClient(serverUrl));
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
        Func<Guid, string, TimeSpan, Uri?, TImplementation> create)
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
                options.Timeout,
                options.ServerUrl);

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

        return (TClient)namedClient;
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

    public IReadOnlyCollection<string> ClientNames => _clientNames;

    private static string GenerateClientLookupKey<T>(string? name = null) => $"{name}_{typeof(T).Name}";

    public delegate void Configure(RelewiseOptionsBuilder builder, IServiceProvider services);
}