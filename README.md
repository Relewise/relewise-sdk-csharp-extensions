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

We recommend that the `DatasetId` and `ApiKey` are stored in a configuration-file. We provide a default way of reading from the appsettings.json, see `options.ReadFromConfiguration(configuration)` below:
```csharp
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", true)
    .Build();
        
services.AddRelewise(options => options.ReadFromConfiguration(configuration));
```

The configuration offers a lot of other great features, such as 
- Set specific options for `DatasetId`, `ApiKey` and `Timeout` for the individual client instances of `ITracker`, `IRecommender` and `ISearcher`.
- Named clients to allow different configuration for integrations etc or to use for a multi site-setup.

Here is a full example of all the configuration settings we provide via the appsettings or via the fluent API:
```json
"Relewise": {
    "DatasetId": "6D9361AA-A23D-4BF2-A818-5ABA792E2102",
    "ApiKey": "r4FqfMqtiZjJmoN",
    "Timeout": "00:00:03",
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
            "DatasetId": "8DF23DAF-6C96-47DB-BE34-84629359D3B8",
            "ApiKey": "61ce444b6e7c4f",
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

## License

Relewise.Client.Extensions is licensed under the [MIT license](./LICENSE)
