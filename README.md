# Relewise.Client.Extensions [![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE) [![NuGet version](https://img.shields.io/nuget/v/Relewise.Client.Extensions)](https://www.nuget.org/packages/Relewise.Client.Extensions) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://https://github.com/Relewise/relewise-sdk-csharp-extensions/pulls)

### Relewise.Client.Extensions

You should install Relewise.Client.Extensions using NuGet:

> Install-Package Relewise.Client.Extensions

Run this command from the NuGet Package Manager Console to install the NuGet package.

## Features

### Dependency Injection / Wiring up Relewise clients

We provide a lot of ways to easily add the clients you need. The default way to do that is using the following code:

```csharp
services.AddRelewise(options =>
{
    options.DatasetId = Guid.Parse("1B5A09DB-561E-47E0-B8ED-4E559A1B7EB9");
    options.ApiKey = "r4FqfMqtiZjJmoN";
    options.Timeout = TimeSpan.FromSeconds(3);
});
```

This will wire up the client instances of `ITracker`, `IRecommender` and `ISearcher`, and these clients will all be configured for the `DatasetId` and `ApiKey` options specificed above, and with a request timeout of 3 seconds.
Optionally, it is possible to define a `ServerUrl` for the client to target. If no value is provided the client will by default target the main Relewise server.

### OAuth 2.0 client credentials

OAuth authentication requires `Relewise.Client` 1.320.0 or later, which is the minimum SDK version required by this library. Configure `ClientId` and `ClientSecret` instead of `ApiKey`:

```csharp
services.AddRelewise(options =>
{
    options.DatasetId = Guid.Parse(configuration["Relewise:DatasetId"]!);
    options.ClientId = Guid.Parse(configuration["Relewise:ClientId"]!);
    options.ClientSecret = configuration["Relewise:ClientSecret"];
    options.ServerUrl = new Uri(configuration["Relewise:ServerUrl"]!);
});
```

The Client ID identifies the OAuth credential and is separate from the Dataset ID. Keep the Client Secret in secure server-side configuration, such as environment variables or a secret store. The SDK handles token acquisition, caching, and renewal; the registered singleton clients are reused across requests.

`ReadFromConfiguration` supports the same options. For example, the following shows the configuration shape; replace the placeholders through your configuration provider:

```json
"Relewise": {
    "DatasetId": "<Dataset ID>",
    "ClientId": "<Client ID>",
    "ClientSecret": "<Client Secret>",
    "ServerUrl": "<Server URL>"
}
```

Both authentication methods work for Tracker, Recommender, Searcher, DataAccessor, SearchAdministrator, and Analyzer, including named clients. Credentials inherit through the existing global, per-client, named, and named per-client options. A child that sets `ApiKey` selects API-key authentication; a child that sets `ClientId` or `ClientSecret` selects OAuth and can inherit missing OAuth fields from its parent. Without inherited OAuth credentials, configure both fields together. Do not set `ApiKey` and OAuth fields at the same configuration level.

For example, an OAuth-based application can use a separate API key for a named integration:

```csharp
services.AddRelewise(options =>
{
    options.ReadFromConfiguration(configuration);
    options.Named.Add("Integration", integration =>
    {
        integration.ApiKey = configuration["Integration:ApiKey"];
        integration.Tracker.Timeout = TimeSpan.FromSeconds(30);
    });
});
```

`IRelewiseClientFactory.GetOptions<TClient>()` returns `ClientId` and `ClientSecret` for OAuth clients, with `ApiKey` set to null. Avoid logging resolved credentials. Existing API-key registrations and the API-key `RelewiseClientOptions` constructor remain supported.

See the [SDK authentication guide](https://docs.relewise.com/docs/developer/sdks/dotnet.html#authentication) for credential setup and OAuth behavior.

### Reading configuration

We recommend that the `DatasetId`, `ApiKey` and `ServerUrl` are stored in a configuration-file. We provide a default way of reading from the appsettings.json, see `options.ReadFromConfiguration(configuration)` below:

```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", true)
    .Build();

services.AddRelewise(options => options.ReadFromConfiguration(configuration));
```

The configuration offers a lot of other great features, such as

- Set specific options for `DatasetId`, `ApiKey`, `ServerUrl` and `Timeout` for the individual client instances of `ITracker`, `IRecommender` and `ISearcher`.
- Named clients to allow different configuration for integrations etc or to use for a multi site-setup.

Here is a full example of all the configuration settings we provide via the appsettings or via the fluent API:

```json
"Relewise": {
    "DatasetId": "<<dataset-id>>",
    "ApiKey": "<<api-key>>",
    "Timeout": "00:00:03",
    "ServerUrl": "<<server-url>>",
    "Tracker": {
        "Timeout": "00:00:10"
    },
    "Recommender": {
        "Timeout": "00:00:05"
    },
    "Searcher": {
        "Timeout": "00:00:10"
    },
    "Named": {
        "Integration": {
            "Tracker": {
                "Timeout": "00:00:30"
            }
        },
        "ContentSite": {
            "DatasetId": "<<dataset-id>>",
            "ApiKey": "<<api-key>>",
            "ServerUrl": "<<server-url>>",
            "Timeout": "00:00:03",
            "Tracker": {
                "Timeout": "00:00:10"
            },
            "Recommender": {
                "Timeout": "00:00:05"
            },
            "Searcher": {
                "Timeout": "00:00:10"
            }
        }
    }
}
```

When using named clients, you can use the `IRelewiseClientFactory` to retrieve an instance of `ITracker`, `IRecommender` and `ISearcher`:

```csharp
IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
ITracker tracker = factory.GetClient<ITracker>("Integration");
```

## Contributing

Pull requests are always welcome.  
Please fork this repository and make a PR when you are ready with your contribution.

Otherwise you are welcome to open an Issue in our [issue tracker](https://github.com/Relewise/relewise-sdk-csharp-extensions/issues).

## Publishing

Publish a GitHub release with a semantic-version tag such as `1.8.0` or `1.8.0-rc.1` (without a `v` prefix). The release tag sets the project and NuGet package version during build and packaging; no version update in the source files is needed. The release commit must be on `main`.

To build and package the same version locally, run `dotnet build --configuration Release /p:Version=1.8.0` followed by `dotnet pack --configuration Release /p:Version=1.8.0 --no-build --output artifacts`.

## License

Relewise.Client.Extensions is licensed under the [MIT license](./LICENSE)
