using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRelewise(this IServiceCollection services, Action<RelewiseOptionsBuilder> configure)
    {
        return services.AddRelewise((builder, _) => configure(builder));
    }

    /// <summary>
    /// Registers services and configures <see cref="RelewiseOptionsBuilder"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/></param>
    /// <param name="configure">A delegate to configure <see cref="RelewiseOptionsBuilder"/></param>
    /// <returns>The <see cref="IServiceCollection"/></returns>
    public static IServiceCollection AddRelewise(this IServiceCollection services, Action<RelewiseOptionsBuilder, IServiceProvider> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        services.AddSingleton(new RelewiseClientFactory.Configure(configure));

        services.TryAddSingleton<IRelewiseClientFactory, RelewiseClientFactory>();

        TryAdd<ITracker, Tracker>(
            services,
            options => options.Tracker,
            (datasetId, apiKey, timeout) => new Tracker(datasetId, apiKey, timeout));

        TryAdd<IRecommender, Recommender>(
            services,
            options => options.Recommender,
            (datasetId, apiKey, timeout) => new Recommender(datasetId, apiKey, timeout));

        TryAdd<ISearcher, Searcher>(
            services,
            options => options.Searcher,
            (datasetId, apiKey, timeout) => new Searcher(datasetId, apiKey, timeout));

        return services;
    }

    private static void TryAdd<TInterface, TClass>(
        IServiceCollection services, 
        Func<RelewiseOptionsBuilder, RelewiseClientOptionsBuilder> clientOptionsProvider, 
        Func<Guid, string, TimeSpan, TClass> create)
        where TInterface : class, IClient
        where TClass : TInterface
    {
        services.TryAddSingleton<TInterface>(provider =>
        {
            var builder = provider.GetRequiredService<RelewiseOptionsBuilder>();

            RelewiseClientOptions? globalOptions = builder.Build();
            RelewiseClientOptions? clientOptions = clientOptionsProvider(builder).Build(globalOptions);

            if (clientOptions == null)
            {
                throw new InvalidOperationException($@"No options were given to create a non-named client for {typeof(TInterface).Name}.

To configure this client, use the 'services.AddRelewise(options => {{ ... }});'-method in your startup code.");
            }

            return create(clientOptions.DatasetId, clientOptions.ApiKey, clientOptions.Timeout);
        });
    }
}