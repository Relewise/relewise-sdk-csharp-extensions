using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relewise.Client.Extensions.DependencyInjection;
using Relewise.Client.Search;

namespace Relewise.Client.Extensions.Tests
{
    [TestFixture]
    public class FromConfigurationTester
    {
        [Test]
        public void ReadFromConfiguration_Default()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration()));

            FromConfigAssertion(serviceCollection);
        }

        [Test]
        public void ReadFromConfiguration_SpecificSection()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "OtherLocation"));

            FromConfigAssertion(serviceCollection);
        }

        [Test]
        public void ReadFromConfiguration_SpecificSectionWithOtherServerUrl()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "Stage"));

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            var tracker = provider.GetService<ITracker>();
            Assert.That(tracker, Is.Not.Null);
            Assert.That(provider.GetService<IRecommender>(), Is.Not.Null);
            Assert.That(provider.GetService<ISearcher>(), Is.Not.Null);

            Assert.That(tracker!.ServerUrl, Is.EqualTo("https://stage01-api.relewise.com/"));
        }

        [Test]
        public void ReadFromConfiguration_SpecificSectionWithInvalidOtherServerUrl()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "InvalidStage"));

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            Assert.Catch<ArgumentException>(() => provider.GetService<ITracker>());
        }

        [Test]
        public void ReadFromConfiguration_WithNamedClients()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration()));

            FromConfigAssertion(serviceCollection);

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
            ITracker tracker = factory.GetClient<ITracker>("ContentSite");

            Assert.That(tracker, Is.Not.Null);
            Assert.That(tracker.DatasetId, Is.EqualTo(Guid.Parse("B57CB490-1556-4F06-AA26-96451533A9B8")));
            Assert.That(tracker.ServerUrl, Is.EqualTo("https://api.relewise.com"));
            Assert.That(tracker.Timeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        }

        [Test]
        public void ReadFromConfiguration_SpecificSectionWithNamedClientsWithOtherServerUrl()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "Stage"));

            ServiceProvider provider = serviceCollection.BuildServiceProvider();
            ITracker? tracker = provider.GetService<ITracker>();

            IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
            ITracker namedClientTracker = factory.GetClient<ITracker>("ContentSite");

            Assert.That(tracker, Is.Not.Null);
            Assert.That(namedClientTracker, Is.Not.Null);
            Assert.That(tracker!.ServerUrl, Is.EqualTo("https://stage01-api.relewise.com/"));
            Assert.That(namedClientTracker.ServerUrl, Is.EqualTo("https://stage02-api.relewise.com/"));
        }

        [Test]
        public void ApiKeySetOnClientButNeverGlobally()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "ApiKeySetOnClientButNeverGlobally"));

            ServiceProvider provider = serviceCollection.BuildServiceProvider();
            var tracker = provider.GetService<ITracker>();
            IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
            RelewiseClientOptions trackerOptions = factory.GetOptions<ITracker>();

            Assert.That(tracker, Is.Not.Null);
            Assert.That(trackerOptions.DatasetId, Is.EqualTo(Guid.Parse("6D9361AA-A23D-4BF2-A818-5ABA792E2102")));
            Assert.That(trackerOptions.ApiKey, Is.EqualTo("r4FqfMqtiZjJmoN"));
        }

        [Test]
        public void ReadFromConfiguration_OnlySetOnClient()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "OnlySetOnTracker"));

            ServiceProvider provider = serviceCollection.BuildServiceProvider();
            var tracker = provider.GetService<ITracker>();
            IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
            RelewiseClientOptions trackerOptions = factory.GetOptions<ITracker>();

            Assert.That(tracker, Is.Not.Null);
            Assert.That(trackerOptions.DatasetId, Is.EqualTo(Guid.Parse("B57CB490-1556-4F06-AA26-96451533A9B8")));
            Assert.That(trackerOptions.ApiKey, Is.EqualTo("61ce444b6e7c4f"));
        }

        [Test]
        public void ReadFromConfiguration_OnlySetOnWrongClient()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration(), "OnlySetOnTracker"));

            ServiceProvider provider = serviceCollection.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => provider.GetService<ISearcher>());
        }

        private static void FromConfigAssertion(IServiceCollection serviceCollection)
        {
            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            var tracker = provider.GetService<ITracker>();
            Assert.That(tracker, Is.Not.Null);
            Assert.That(provider.GetService<IRecommender>(), Is.Not.Null);
            Assert.That(provider.GetService<ISearcher>(), Is.Not.Null);

            Assert.That(tracker!.DatasetId, Is.EqualTo(Guid.Parse("6D9361AA-A23D-4BF2-A818-5ABA792E2102")));
            Assert.That(tracker.ServerUrl, Is.EqualTo("https://api.relewise.com"));
            Assert.That(tracker.Timeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        }

        private static IConfiguration BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
        }
    }
}