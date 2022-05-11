using System;
using Microsoft.Extensions.DependencyInjection;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRelewise(this IServiceCollection services, Action<RelewiseOptions> options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        var config = new RelewiseOptions();
        options.Invoke(config);

        services.AddSingleton(config);
        services.AddSingleton<IRelewiseClientFactory, RelewiseClientFactory>();

        if (!string.IsNullOrWhiteSpace(config.ApiKey) && config.DatasetId.HasValue)
        {
            services.AddSingleton<ITracker>(new Tracker(config.DatasetId.GetValueOrDefault(), config.ApiKey, GetTimeout(config, () => config.Tracker.Timeout)));
            services.AddSingleton<IRecommender>(new Recommender(config.DatasetId.GetValueOrDefault(), config.ApiKey, GetTimeout(config, () => config.Recommender.Timeout)));
            services.AddSingleton<ISearcher>(new Searcher(config.DatasetId.GetValueOrDefault(), config.ApiKey, GetTimeout(config, () => config.Searcher.Timeout)));
        }
        else if (config.Named.Clients.Count == 0)
        {
            // TODO perform better validation
            throw new ArgumentException("The provided options was not in a valid state, either provide a global dataset and ApiKey or provide at least 1 valid named client");
        }

        return services;
    }

    private static TimeSpan GetTimeout(RelewiseOptions options, Func<TimeSpan?> fromSpecificClient)
    {
        return fromSpecificClient() ?? options.Timeout ?? TimeSpan.FromSeconds(5);
    }
}