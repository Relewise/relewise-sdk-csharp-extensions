using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Relewise.Client.Extensions.Infrastructure.Extensions;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.DependencyInjection;

internal class RelewiseClientFactory : IRelewiseClientFactory
{
    private readonly IReadOnlyDictionary<string, IClient> _clients;
    private readonly RelewiseOptions _options;
    private readonly IServiceProvider _provider;

    public RelewiseClientFactory(RelewiseOptions options, IServiceProvider provider)
    {
        _options = options;
        _provider = provider;

        var clients = new Dictionary<string, IClient>();
        foreach ((string name, ClientOptions clientOptions) in options.Named.Clients.AsTuples())
        {
            clients.Add(GenerateClientLookupKey<ISearcher>(name), new Searcher(
                clientOptions.DatasetId ?? options.DatasetId ?? Guid.Empty,
                clientOptions.ApiKey ?? options.ApiKey,
                options.GetTimeout(() => clientOptions.Searcher.Timeout ?? clientOptions.Timeout)));

            clients.Add(GenerateClientLookupKey<ITracker>(name), new Tracker(
                clientOptions.DatasetId ?? options.DatasetId ?? Guid.Empty,
                clientOptions.ApiKey ?? options.ApiKey,
                options.GetTimeout(() => clientOptions.Tracker.Timeout ?? clientOptions.Timeout)));

            clients.Add(GenerateClientLookupKey<IRecommender>(name), new Recommender(
                clientOptions.DatasetId ?? options.DatasetId ?? Guid.Empty,
                clientOptions.ApiKey ?? options.ApiKey,
                options.GetTimeout(() => clientOptions.Recommender.Timeout ?? clientOptions.Timeout)));
        }

        _clients = clients;
    }

    public T GetClient<T>() where T : IClient
    {
        T? client = _provider.GetService<T>();
        if (client == null)
            throw new ArgumentException("No client was registered during startup");

        return client;
    }

    public T GetClient<T>(string name) where T : IClient
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("value can not be null or empty", nameof(name));

        if (!_options.Named.Clients.ContainsKey(name))
            throw new ArgumentException($"No clients with name '{name}' was registered during startup");

        if (_clients.TryGetValue(GenerateClientLookupKey<T>(name), out IClient client))
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("Expected generic 'T' to be an interface");

            throw new ArgumentException($"No clients with name '{name}' was registered during startup");
        }

        return (T)client!;
    }

    private static string GenerateClientLookupKey<T>(string name) => $"{name}_{typeof(T).Name}";
}