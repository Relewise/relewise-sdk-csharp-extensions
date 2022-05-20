using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Relewise.Client.Extensions.Infrastructure.Extensions;

namespace Relewise.Client.Extensions;

public class RelewiseOptionsBuilder : RelewiseClientsOptionsBuilder
{
    /// <summary>
    /// Allows you to provide named clients, to support different configuration, e.g. Timeout, for different purposes.
    /// In more advanced scenarios, this also allows you to support multi-site configuration, targeting different datasets within Relewise.
    /// </summary>
    public NamedBuilder Named { get; } = new();

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
            { sectionName, new JsonConfiguration { DatasetId = Guid.Empty, ApiKey = "<ApiKey>", Timeout = TimeSpan.FromSeconds(3) } }
        };

        return @$"

Example (to be used in e.g. appSettings.json):

{JsonConvert.SerializeObject(configuration, settings)}";
    }
    
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

        public Dictionary<string, RelewiseClientsOptionsBuilder>? Named { get; set; }

        internal void Map(RelewiseOptionsBuilder options)
        {
            options.ApiKey = ApiKey;
            options.DatasetId = DatasetId;
            options.Timeout = Timeout;

            options.Tracker.DatasetId = Tracker?.DatasetId;
            options.Tracker.ApiKey = Tracker?.ApiKey;
            options.Tracker.Timeout = Tracker?.Timeout;

            options.Searcher.DatasetId = Searcher?.DatasetId;
            options.Searcher.ApiKey = Searcher?.ApiKey;
            options.Searcher.Timeout = Searcher?.Timeout;

            options.Recommender.DatasetId = Recommender?.DatasetId;
            options.Recommender.ApiKey = Recommender?.ApiKey;
            options.Recommender.Timeout = Recommender?.Timeout;

            if (Named is { Count: > 0 })
            {
                foreach ((string name, RelewiseClientsOptionsBuilder namedOptions) in Named.AsTuples())
                {
                    options.Named.Add(name, named =>
                    {
                        named.ApiKey = namedOptions.ApiKey;
                        named.DatasetId = namedOptions.DatasetId;
                        named.Timeout = namedOptions.Timeout;
                    }, throwIfExists: true);
                }
            }
        }
    }

    private class ClientJsonConfiguration
    {
        public Guid? DatasetId { get; set; }
        public string? ApiKey { get; set; }
        public TimeSpan? Timeout { get; set; }
    }

    internal override void Reset()
    {
        base.Reset();

        Named.Clients.Clear();
    }
}

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

    internal override void Reset()
    {
        base.Reset();

        Tracker.Reset();
        Recommender.Reset();
        Searcher.Reset();
    }
}

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
    
    internal virtual RelewiseClientOptions? Build(RelewiseClientOptions? parentOptions = null)
    {
        if (DatasetId == null && parentOptions == null)
            return null; // Dataset has not been configured - so we cannot create any options (which is okay)

        Guid datasetId = DatasetId.GetValueOrDefault(parentOptions?.DatasetId ?? Guid.Empty);

        if (datasetId.Equals(Guid.Empty))
            throw new ArgumentOutOfRangeException($"Value for '{nameof(DatasetId)} cannot be an empty Guid. The correct value can be found using https://my.relewise.com.");

        string? apiKey = ApiKey ?? parentOptions?.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException($@"Value for '{nameof(ApiKey)} cannot be null or empty. The correct value can be found using https://my.relewise.com.", nameof(ApiKey));

        TimeSpan timeout = Timeout.GetValueOrDefault(parentOptions?.Timeout ?? TimeSpan.FromSeconds(5));

        return new RelewiseClientOptions(datasetId, apiKey!, timeout);
    }

    internal virtual void Reset()
    {
        DatasetId = null;
        ApiKey = null;
        Timeout = null;
    }
    
    internal void Initialize(RelewiseClientOptions options)
    {
        DatasetId = options.DatasetId;
        ApiKey = options.ApiKey;
        Timeout = options.Timeout;
    }
}