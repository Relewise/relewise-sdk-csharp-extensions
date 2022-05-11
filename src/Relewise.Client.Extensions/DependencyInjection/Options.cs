using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Relewise.Client.Extensions.Infrastructure.Extensions;

namespace Relewise.Client.Extensions.DependencyInjection;

public class DefaultOptions
{
    public Guid? DatasetId { get; set; }
    public string? ApiKey { get; set; }
    public TimeSpan? Timeout { get; set; }
}

public class ClientOptions : DefaultOptions
{
    public DefaultOptions Tracker { get; } = new();
    public DefaultOptions Recommender { get; } = new();
    public DefaultOptions Searcher { get; } = new();
}

public class RelewiseOptions : ClientOptions
{
    public NamedClients Named { get; } = new();

    public void ReadFromConfiguration(IConfiguration configuration, string sectionName = "Relewise")
    {
        var relewiseSection = configuration.GetSection(sectionName);
        if (relewiseSection == null)
            throw new ArgumentException("The specified section was not found", nameof(sectionName));

        var readOptions = relewiseSection.Get<RelewiseJsonConfiguration>();
        if (readOptions == null)
            throw new InvalidOperationException("Could not read options from configuration file");

        readOptions.Map(this);
    }

    internal TimeSpan GetTimeout(Func<TimeSpan?> fromSpecificClient)
    {
        return fromSpecificClient() ?? Timeout ?? TimeSpan.FromSeconds(5);
    }

    public class NamedClients
    {
        internal Dictionary<string, ClientOptions> Clients { get; } = new();

        public void Add(string name, Action<ClientOptions> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (Clients.ContainsKey(name))
                throw new ArgumentException("A client with that name was already registered", nameof(name));

            var clientOptions = new ClientOptions();
            options.Invoke(clientOptions);

            Clients.Add(name, clientOptions);
        }
    }
}

internal class RelewiseJsonConfiguration : SharedRelewiseJsonConfiguration
{
    public SharedRelewiseJsonConfiguration? Tracker { get; }
    public SharedRelewiseJsonConfiguration? Recommender { get; }
    public SharedRelewiseJsonConfiguration? Searcher { get; }

    public Dictionary<string, ClientOptions>? Clients { get; set; }

    internal void Map(RelewiseOptions options)
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

        if (Clients is { Count: > 0 })
        {
            foreach ((string name, ClientOptions clientOptions) in Clients.AsTuples())
            {
                options.Named.Add(name, opt =>
                {
                    opt.ApiKey = clientOptions.ApiKey;
                    opt.DatasetId = clientOptions.DatasetId;
                    opt.Timeout = clientOptions.Timeout;
                });
            }
        }
    }
}

internal class SharedRelewiseJsonConfiguration
{
    public Guid? DatasetId { get; set; }
    public string? ApiKey { get; set; }
    public TimeSpan? Timeout { get; set; }
}
