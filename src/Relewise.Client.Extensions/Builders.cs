using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Relewise.Client.Extensions.Infrastructure.Extensions;

namespace Relewise.Client.Extensions;

/// <summary>
/// Represents the root configuration of Relewise.
/// </summary>
public class RelewiseOptionsBuilder : RelewiseClientsOptionsBuilder
{
    /// <summary>
    /// Allows you to provide named clients, to support different configuration, e.g. Timeout, for different purposes.
    /// In more advanced scenarios, this also allows you to support multi-site configuration, targeting different datasets within Relewise.
    /// </summary>
    public NamedBuilder Named { get; } = new();

    /// <summary>
    /// Reads and sets all options from the <see cref="IConfiguration"/> instance.
    /// Simply just parse the instance typically available in the Startup/Bootstrap code of your application.
    /// </summary>
    public RelewiseOptionsBuilder ReadFromConfiguration(IConfiguration configuration, string sectionName = "Relewise")
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentException(@"Value cannot be null or empty", nameof(sectionName));

        IConfigurationSection? relewiseSection = configuration.GetSection(sectionName);

        if (relewiseSection == null)
            throw new ArgumentException($"The specified section '{sectionName}' was not found.{ProvideExample(sectionName)}", nameof(sectionName));

        var readOptions = relewiseSection.Get<JsonConfiguration>();

        if (readOptions == null)
            throw new InvalidOperationException($"Could not read Relewise configuration from configuration. Expected section: '{sectionName}'.{ProvideExample(sectionName)}");

        readOptions.Map(this);

        return this;
    }

    private static string ProvideExample(string sectionName)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        var configuration = new Dictionary<string, JsonConfiguration>
        {
            { sectionName, new JsonConfiguration { DatasetId = Guid.Empty, ApiKey = "<ApiKey>", Timeout = TimeSpan.FromSeconds(3), ServerUrl = new Uri("<ServerUrl>")} }
        };

        return @$"

Example (to be used in e.g. appSettings.json):

{JsonConvert.SerializeObject(configuration, settings)}";
    }

    /// <summary>
    /// Represents configuration of named clients, if you're using this functionality.
    /// Named clients allows you to configure access to multiple datasets from the same application, e.g. for multi-site purposes.
    /// Named clients also allows you to override e.g. timeout of default clients - this is typically handy for integration scenarios.
    /// </summary>
    public class NamedBuilder
    {
        internal Dictionary<string, RelewiseClientsOptionsBuilder> Clients { get; } = new();

        /// <summary>
        /// Adds a named client, where you can override options for the different clients, e.g. timeout.
        /// </summary>
        /// <param name="name">Name of the client. You'll use this name at runtime with the <see cref="IRelewiseClientFactory"/>-instance.</param>
        /// <param name="options">The options you'd like to set for this named client.</param>
        /// <param name="throwIfExists">Defines whether the method should throw, if a client with same name exists.</param>
        public void Add(string name, Action<RelewiseClientsOptionsBuilder> options, bool throwIfExists = false)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (Clients.ContainsKey(name) && throwIfExists)
                throw new ArgumentException("A client with that name was already registered", nameof(name));

            if (Clients.TryGetValue(name, out RelewiseClientsOptionsBuilder builder))
            {
                if (throwIfExists)
                    throw new ArgumentException("A client with that name was already registered", nameof(name));
            }
            else
            {
                builder = new RelewiseClientsOptionsBuilder();
                Clients.Add(name, builder);
            }

            options.Invoke(builder);
        }
    }

    private class JsonConfiguration : ClientJsonConfiguration
    {
        public ClientJsonConfiguration? Tracker { get; }
        public ClientJsonConfiguration? Recommender { get; }
        public ClientJsonConfiguration? Searcher { get; }
        public ClientJsonConfiguration? DataAccessor { get; }
        public ClientJsonConfiguration? SearchAdministrator { get; }
        public ClientJsonConfiguration? Analyzer { get; }

        public Dictionary<string, RelewiseClientsOptionsBuilder>? Named { get; set; }

        internal void Map(RelewiseOptionsBuilder options)
        {
            options.ApiKey = ApiKey;
            options.DatasetId = DatasetId;
            options.Timeout = Timeout;
            options.ServerUrl = ServerUrl;

            MapClientConfig(options.Tracker, Tracker);
            MapClientConfig(options.Searcher, Searcher);
            MapClientConfig(options.Recommender, Recommender);
            MapClientConfig(options.DataAccessor, DataAccessor);
            MapClientConfig(options.SearchAdministrator, SearchAdministrator);
            MapClientConfig(options.Analyzer, Analyzer);

            if (Named is { Count: > 0 })
            {
                foreach ((string name, RelewiseClientsOptionsBuilder namedOptions) in Named.AsTuples())
                {
                    options.Named.Add(name, named =>
                    {
                        named.ApiKey = namedOptions.ApiKey;
                        named.DatasetId = namedOptions.DatasetId;
                        named.Timeout = namedOptions.Timeout;
                        named.ServerUrl = namedOptions.ServerUrl;

                        MapClientConfig(named.Tracker, namedOptions.Tracker);
                        MapClientConfig(named.Searcher, namedOptions.Searcher);
                        MapClientConfig(named.Recommender, namedOptions.Recommender);
                        MapClientConfig(named.DataAccessor, namedOptions.DataAccessor);
                        MapClientConfig(named.SearchAdministrator, namedOptions.SearchAdministrator);
                        MapClientConfig(named.Analyzer, namedOptions.Analyzer);

                    }, throwIfExists: true);
                }
            }
        }

        private static void MapClientConfig(RelewiseClientOptionsBuilder options, ClientJsonConfiguration? config)
        {
            options.DatasetId = config?.DatasetId;
            options.ApiKey = config?.ApiKey;
            options.Timeout = config?.Timeout;
            if (config?.ServerUrl != null) options.ServerUrl = config.ServerUrl;
        }

        private static void MapClientConfig(RelewiseClientOptionsBuilder options, RelewiseClientOptionsBuilder config)
        {
            options.DatasetId = config.DatasetId;
            options.ApiKey = config.ApiKey;
            options.Timeout = config.Timeout;

            if (config.ServerUrl != null)
                options.ServerUrl = config.ServerUrl;
        }
    }

    private class ClientJsonConfiguration
    {
        public Guid? DatasetId { get; set; }
        public string? ApiKey { get; set; }
        public TimeSpan? Timeout { get; set; }
        public Uri? ServerUrl { get; set; }
    }
}

/// <summary>
/// Represents options available to the different clients of Relewise.
/// </summary>
public class RelewiseClientsOptionsBuilder : RelewiseClientOptionsBuilder
{
    /// <summary>
    /// Defines options for the <see cref="Relewise.Client.ITracker"/> client.
    /// If no options have been provided, the client will inherit options from the root configuration.
    /// </summary>
    public RelewiseClientOptionsBuilder Tracker { get; } = new();

    /// <summary>
    /// Defines options for the <see cref="Relewise.Client.IRecommender"/> client.
    /// If no options have been provided, the client will inherit options from the root configuration.
    /// </summary>
    public RelewiseClientOptionsBuilder Recommender { get; } = new();

    /// <summary>
    /// Defines options for the <see cref="Relewise.Client.Search.ISearcher"/> client.
    /// If no options have been provided, the client will inherit options from the root configuration.
    /// </summary>
    public RelewiseClientOptionsBuilder Searcher { get; } = new();

    /// <summary>
    /// Defines options for the <see cref="Relewise.Client.IDataAccessor"/> client.
    /// If no options have been provided, the client will inherit options from the root configuration.
    /// </summary>
    public RelewiseClientOptionsBuilder DataAccessor { get; } = new();

    /// <summary>
    /// Defines options for the <see cref="Relewise.Client.Search.ISearchAdministrator"/> client.
    /// If no options have been provided, the client will inherit options from the root configuration.
    /// </summary>
    public RelewiseClientOptionsBuilder SearchAdministrator { get; } = new();

    /// <summary>
    /// Defines options for the <see cref="Relewise.Client.IAnalyzer"/> client.
    /// If no options have been provided, the client will inherit options from the root configuration.
    /// </summary>
    public RelewiseClientOptionsBuilder Analyzer { get; } = new();
}

/// <summary>
/// Represents the options available for a single client.
/// </summary>
public class RelewiseClientOptionsBuilder
{
    /// <summary>
    /// Provides the Dataset Id to be used by this client.
    /// Value can be found here: https://my.relewise.com.
    /// </summary>
    public Guid? DatasetId { get; set; }

    /// <summary>
    /// Provides the API-key to be used by this client.
    /// Value can be found here: https://my.relewise.com.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Timeout to be used when requesting the Relewise API by this client.
    /// Value should be greater than zero.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Provides the url of the server to be used by this client.
    /// Value can be found here: https://my.relewise.com.
    /// </summary>
    public Uri? ServerUrl { get; set; }

    internal virtual RelewiseClientOptions? Build(RelewiseClientOptions? parentOptions = null)
    {
        if (DatasetId == null && parentOptions == null)
            return null; // Dataset has not been configured - so we cannot create any options (which is okay)

        Guid datasetId = DatasetId.GetValueOrDefault(parentOptions?.DatasetId ?? Guid.Empty);

        if (datasetId.Equals(Guid.Empty))
            throw new ArgumentOutOfRangeException($"Value for '{nameof(DatasetId)} cannot be an empty Guid. The correct value can be found using https://my.relewise.com.");

        string? apiKey = ApiKey ?? parentOptions?.ApiKey;

        if (apiKey is null || string.IsNullOrWhiteSpace(apiKey)) // compiler is not happy about only having the string.IsNullOrWhiteSpace-check
            throw new ArgumentException($@"Value for '{nameof(ApiKey)} cannot be null or empty. The correct value can be found using https://my.relewise.com.", nameof(ApiKey));

        TimeSpan timeout = Timeout.GetValueOrDefault(parentOptions?.Timeout ?? TimeSpan.FromSeconds(5));

        var serverUrl = ServerUrl ?? parentOptions?.ServerUrl;

        return new RelewiseClientOptions(datasetId, apiKey, timeout, serverUrl);
    }

    internal void Initialize(RelewiseClientOptions options)
    {
        DatasetId = options.DatasetId;
        ApiKey = options.ApiKey;
        Timeout = options.Timeout;
        ServerUrl = options.ServerUrl;
    }
}