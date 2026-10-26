using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relewise.Client.DataTypes;
using Relewise.Client.Extensions.DependencyInjection;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.Tests;

[TestFixture]
public class OAuthOptionsTester
{
    private static readonly Guid DatasetId = Guid.Parse("0f805bb9-72ba-4c69-a3dd-5927d4523294");
    private static readonly Guid ClientId = Guid.Parse("526934f3-099a-4e2e-ae21-460ef5021401");
    private static readonly Uri ServerUrl = new("https://dataset.example.test/");

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void AllSixClients_SupportBothAuthenticationMethods(bool oauth, bool named)
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            options.DatasetId = DatasetId;
            options.ServerUrl = ServerUrl;
            options.Timeout = TimeSpan.FromSeconds(17);
            if (oauth)
            {
                options.ClientId = ClientId;
                options.ClientSecret = "test-secret";
            }
            else
                options.ApiKey = "test-api-key";

            if (named) options.Named.Add("Integration", _ => { });
        });
        var factory = provider.GetRequiredService<IRelewiseClientFactory>();
        string? name = named ? "Integration" : null;
        IClient[] clients = GetClients(factory, name);

        foreach (IClient client in clients)
        {
            Assert.That(client.DatasetId, Is.EqualTo(DatasetId));
            Assert.That(client.ServerUrl, Is.EqualTo(ServerUrl.ToString()));
            Assert.That(client.Timeout, Is.EqualTo(TimeSpan.FromSeconds(17)));
        }
        Assert.That(factory.GetClient<ITracker>(name), Is.SameAs(clients[0]));
        RelewiseClientOptions resolved = factory.GetOptions<ITracker>(name);
        Assert.That(resolved.ApiKey, Is.EqualTo(oauth ? null : "test-api-key"));
        Assert.That(resolved.ClientId, Is.EqualTo(oauth ? ClientId : (Guid?)null));
        Assert.That(resolved.ClientSecret, Is.EqualTo(oauth ? "test-secret" : null));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClientCredentials_CanBeConfiguredOnlyOnSpecificClients(bool named)
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            options.DatasetId = DatasetId;
            options.ServerUrl = ServerUrl;
            if (named)
                options.Named.Add("Integration", clients => SetOAuth(clients.Tracker));
            else
                SetOAuth(options.Tracker);
        });
        var factory = provider.GetRequiredService<IRelewiseClientFactory>();
        string? name = named ? "Integration" : null;
        Assert.That(factory.GetClient<ITracker>(name).DatasetId, Is.EqualTo(DatasetId));
        Assert.That(factory.Contains<ISearcher>(name), Is.False);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void SpecificClient_CanSwitchAuthenticationMethods(bool parentOAuth, bool named)
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            options.DatasetId = DatasetId;
            if (parentOAuth) SetOAuth(options);
            else options.ApiKey = "parent-api-key";

            void Override(RelewiseClientOptionsBuilder client)
            {
                if (parentOAuth) client.ApiKey = "override-api-key";
                else SetOAuth(client);
            }
            if (named) options.Named.Add("Integration", clients => Override(clients.Tracker));
            else Override(options.Tracker);
        });
        var factory = provider.GetRequiredService<IRelewiseClientFactory>();
        string? name = named ? "Integration" : null;
        RelewiseClientOptions resolved = factory.GetOptions<ITracker>(name);
        Assert.That(resolved.ApiKey, Is.EqualTo(parentOAuth ? "override-api-key" : null));
        Assert.That(resolved.ClientId, Is.EqualTo(parentOAuth ? null : (Guid?)ClientId));
        Assert.That(resolved.ClientSecret, Is.EqualTo(parentOAuth ? null : "test-secret"));
        Assert.That(factory.GetClient<ITracker>(name).ServerUrl, Is.EqualTo("https://api.relewise.com"));
    }

    [Test]
    public void NamedAndSpecificOverrides_InheritOAuthAndOverrideSecret()
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            options.DatasetId = DatasetId;
            SetOAuth(options);
            options.Tracker.Timeout = TimeSpan.FromSeconds(11);
            options.Named.Add("Integration", clients =>
            {
                clients.ServerUrl = ServerUrl;
                clients.ClientSecret = "named-secret";
                clients.Tracker.ClientSecret = "tracker-secret";
            });
        });
        var factory = provider.GetRequiredService<IRelewiseClientFactory>();
        RelewiseClientOptions tracker = factory.GetOptions<ITracker>("Integration");
        Assert.That(tracker.ClientId, Is.EqualTo(ClientId));
        Assert.That(tracker.ClientSecret, Is.EqualTo("tracker-secret"));
        Assert.That(tracker.Timeout, Is.EqualTo(TimeSpan.FromSeconds(11)));
        Assert.That(tracker.ServerUrl, Is.EqualTo(ServerUrl));
        Assert.That(factory.GetOptions<ISearcher>("Integration").ClientSecret, Is.EqualTo("named-secret"));
    }

    [TestCase("")]
    [TestCase("Tracker")]
    [TestCase("Recommender")]
    [TestCase("Searcher")]
    [TestCase("DataAccessor")]
    [TestCase("SearchAdministrator")]
    [TestCase("Analyzer")]
    [TestCase("Named.Integration")]
    [TestCase("Named.Integration.Tracker")]
    [TestCase("Named.Integration.Recommender")]
    [TestCase("Named.Integration.Searcher")]
    [TestCase("Named.Integration.DataAccessor")]
    [TestCase("Named.Integration.SearchAdministrator")]
    [TestCase("Named.Integration.Analyzer")]
    public void JsonConfiguration_MapsOAuthAtEverySupportedLevel(string path)
    {
        string credentials = $"\"ClientId\":\"{ClientId}\",\"ClientSecret\":\"json-secret\"";
        string nested = path.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Reverse().Aggregate(credentials, (value, section) => $"\"{section}\":{{{value}}}");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            $"{{\"CustomRelewise\":{{\"DatasetId\":\"{DatasetId}\",\"ServerUrl\":\"{ServerUrl}\",{nested}}}}}"));
        IConfiguration configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        using ServiceProvider provider = CreateProvider(options => options.ReadFromConfiguration(configuration, "CustomRelewise"));
        var factory = provider.GetRequiredService<IRelewiseClientFactory>();
        string? name = path.StartsWith("Named.") ? "Integration" : null;
        string client = path.Split('.').Last();
        RelewiseClientOptions resolved = client switch
        {
            "Recommender" => factory.GetOptions<IRecommender>(name),
            "Searcher" => factory.GetOptions<ISearcher>(name),
            "DataAccessor" => factory.GetOptions<IDataAccessor>(name),
            "SearchAdministrator" => factory.GetOptions<ISearchAdministrator>(name),
            "Analyzer" => factory.GetOptions<IAnalyzer>(name),
            _ => factory.GetOptions<ITracker>(name)
        };
        Assert.That(resolved.ClientId, Is.EqualTo(ClientId));
        Assert.That(resolved.ClientSecret, Is.EqualTo("json-secret"));
        Assert.That(resolved.ApiKey, Is.Null);
        Assert.That(resolved.ServerUrl, Is.EqualTo(ServerUrl));
    }

    [TestCase("missing-id")]
    [TestCase("missing-secret")]
    [TestCase("empty-id")]
    [TestCase("blank-secret")]
    [TestCase("mixed")]
    public void InvalidCredentials_AreRejectedWithoutLeakingSecrets(string scenario)
    {
        var options = new RelewiseClientOptionsBuilder { DatasetId = DatasetId };
        SetOAuth(options);
        switch (scenario)
        {
            case "missing-id": options.ClientId = null; break;
            case "missing-secret": options.ClientSecret = null; break;
            case "empty-id": options.ClientId = Guid.Empty; break;
            case "blank-secret": options.ClientSecret = " "; break;
            case "mixed": options.ApiKey = "test-api-key"; break;
        }
        ArgumentException? error = Assert.Catch<ArgumentException>(() => options.Build());
        Assert.That(error!.Message, Does.Not.Contain("test-secret").And.Not.Contain("test-api-key"));
    }

    [Test]
    public void OAuthOptions_SupportInitializationEqualityAndHashing()
    {
        var first = new RelewiseClientOptions(DatasetId, ClientId, "test-secret", TimeSpan.FromSeconds(5));
        var builder = new RelewiseClientOptionsBuilder();
        builder.Initialize(first);
        RelewiseClientOptions? copy = builder.Build();
        Assert.That(copy, Is.EqualTo(first));
        Assert.That(copy!.GetHashCode(), Is.EqualTo(first.GetHashCode()));
        Assert.That(first, Is.Not.EqualTo(new RelewiseClientOptions(DatasetId, ClientId, "different-secret", first.Timeout)));
        Assert.That(first, Is.Not.EqualTo(new RelewiseClientOptions(DatasetId, Guid.NewGuid(), "test-secret", first.Timeout)));
        Assert.That(first, Is.Not.EqualTo(new RelewiseClientOptions(DatasetId, "test-api-key", first.Timeout)));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task ResolvedTracker_UsesSelectedAuthenticationAndCachesToken(bool oauth, bool named)
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            options.DatasetId = DatasetId;
            options.ServerUrl = ServerUrl;
            if (oauth) SetOAuth(options);
            else options.ApiKey = "test-api-key";
            if (named) options.Named.Add("Integration", _ => { });
        });
        var factory = provider.GetRequiredService<IRelewiseClientFactory>();
        var tracker = (Tracker)factory.GetClient<ITracker>(named ? "Integration" : null);
        using var handler = new AuthenticationHandler(oauth);
        using var httpClient = new HttpClient(handler);
        tracker.OverriddenHttpClient = httpClient;
        await tracker.TrackAsync(new ProductView(User.Anonymous(), new Product("test-product")));
        await tracker.TrackAsync(new ProductView(User.Anonymous(), new Product("test-product")));
        Assert.That(handler.TokenRequests, Is.EqualTo(oauth ? 1 : 0));
        Assert.That(handler.ApiRequests, Is.EqualTo(2));
    }

    private sealed class AuthenticationHandler(bool oauth) : HttpMessageHandler
    {
        public int TokenRequests { get; private set; }
        public int ApiRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.That(request.RequestUri!.GetLeftPart(UriPartial.Authority), Is.EqualTo(ServerUrl.GetLeftPart(UriPartial.Authority)));
            if (request.RequestUri.AbsolutePath.EndsWith("/oauth/token"))
            {
                TokenRequests++;
                Assert.That(oauth, Is.True);
                Assert.That(request.RequestUri.AbsolutePath, Is.EqualTo($"/{DatasetId}/oauth/token"));
                Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo("Basic"));
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
                Assert.That(credentials, Is.EqualTo($"{ClientId}:test-secret"));
                Assert.That(await request.Content!.ReadAsStringAsync(cancellationToken), Is.EqualTo("grant_type=client_credentials"));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"test-token\",\"token_type\":\"Bearer\",\"expires_in\":300}", Encoding.UTF8, "application/json")
                };
            }

            ApiRequests++;
            Assert.That(request.Headers.Authorization!.Scheme, Is.EqualTo(oauth ? "Bearer" : "APIKey"));
            Assert.That(request.Headers.Authorization.Parameter, Is.EqualTo(oauth ? "test-token" : "test-api-key"));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static void SetOAuth(RelewiseClientOptionsBuilder options)
    {
        options.ClientId = ClientId;
        options.ClientSecret = "test-secret";
    }

    private static ServiceProvider CreateProvider(Action<RelewiseOptionsBuilder> configure) =>
        new ServiceCollection().AddRelewise(configure).BuildServiceProvider();

    private static IClient[] GetClients(IRelewiseClientFactory factory, string? name) =>
        new IClient[]
        {
            factory.GetClient<ITracker>(name), factory.GetClient<IRecommender>(name),
            factory.GetClient<ISearcher>(name), factory.GetClient<IDataAccessor>(name),
            factory.GetClient<ISearchAdministrator>(name), factory.GetClient<IAnalyzer>(name)
        };
}
