using System;
using System.Collections.Generic;

namespace Relewise.Client.Extensions;

/// <summary>
/// The Service Locator / factory from where you can access all configured clients.
/// In instance of this interface can be located through the <see cref="IServiceProvider"/> instance directly - or indirectly through dependency injection.
/// </summary>
public interface IRelewiseClientFactory
{
    /// <summary>
    /// Provides access to a specific client.
    /// </summary>
    /// <typeparam name="TClient">Defines which client you'd like to access, e.g. <see cref="ITracker"/>.</typeparam>
    /// <param name="name">Optional parameter if you are accessing a named client.</param>
    TClient GetClient<TClient>(string? name = null) where TClient : class, IClient;

    /// <summary>
    /// Provides access to the options used to configure the specific client.
    /// </summary>
    /// <typeparam name="TClient">Defines for which client you'd like to access options, e.g. <see cref="ITracker"/>.</typeparam>
    /// <param name="name">Optional parameter if you are accessing options for a named client.</param>
    RelewiseClientOptions GetOptions<TClient>(string? name = null) where TClient : class, IClient;

    /// <summary>
    /// Provides access to all options configured for all the named clients
    /// </summary>
    IReadOnlyDictionary<string, RelewiseClientOptions> NamedClientOptions { get; }
}