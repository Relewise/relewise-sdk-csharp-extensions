using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRelewise(this IServiceCollection services, Action<RelewiseOptions> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var options = new RelewiseOptions();
        configure.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IRelewiseClientFactory, RelewiseClientFactory>();

        if (options.IsConfigurationValid())
        {
            services.AddSingleton<ITracker>(new Tracker(options.DatasetId.GetValueOrDefault(), options.ApiKey, options.GetTimeout(() => options.Tracker.Timeout)));
            services.AddSingleton<IRecommender>(new Recommender(options.DatasetId.GetValueOrDefault(), options.ApiKey, options.GetTimeout(() => options.Recommender.Timeout)));
            services.AddSingleton<ISearcher>(new Searcher(options.DatasetId.GetValueOrDefault(), options.ApiKey, options.GetTimeout(() => options.Searcher.Timeout)));
        }
        else if (options.Named.Clients.Count == 0 || options.Named.Clients.Any(x => !x.Value.IsClientsValid()))
        {
            throw new ArgumentException("The provided options was not in a valid state, either provide a global dataset and ApiKey or provide at least 1 valid named client");
        }

        return services;
    }
}