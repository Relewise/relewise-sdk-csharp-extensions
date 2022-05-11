# Relewise.Client.Extensions

### Relewise.Client.Extensions

You should install Relewise.Client.Extensions with NuGet:

> Install-Package Relewise.Client.Extensions

Run this command from the NuGet Package Manager Console to install the NuGet package.

## Features

### Wiring up the SDK Client

We provide a lot of ways to easily add the clients you need. The standard way to do that is using the following code:
```csharp
services.AddRelewise(options =>
     {
         options.DatasetId = Guid.Parse("1B5A09DB-561E-47E0-B8ED-4E559A1B7EB9");
         options.ApiKey = "r4FqfMqtiZjJmoN";
         options.Timeout = TimeSpan.FromSeconds(3);
     });
```

You should read the Dataset Id and API key from a configuration-file. Which is also posible to do easily:
```csharp
services
    .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration()));
```

The configuration offers a lot of nifty features, should as 
- specific dataset, apikey or timeout of either the tracker, the recommender or the searcher.
- Named clients to allow different configuration for integrations or a multi site-setup.

## Contributing

Pull requests are always welcome.  
Please fork this repository and make a PR when you are ready with your contribution.  

Otherwise you are welcome to open an Issue in our [issue tracker](https://github.com/Relewise/relewise-sdk-csharp-extensions/issues).

## License

Relewise.Integrations.Umbraco is under the [MIT licensed](./LICENSE)
