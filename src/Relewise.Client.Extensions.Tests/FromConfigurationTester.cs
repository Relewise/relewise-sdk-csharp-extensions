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
        public void ReadFromConfiguration_WithNamedClients()
        {
            var serviceCollection = new ServiceCollection()
                .AddRelewise(options => options.ReadFromConfiguration(BuildConfiguration()));

            FromConfigAssertion(serviceCollection);

            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            IRelewiseClientFactory factory = provider.GetRequiredService<IRelewiseClientFactory>();
            ITracker tracker = factory.GetClient<ITracker>("ContentSite");

            Assert.IsNotNull(tracker);
            Assert.AreEqual(Guid.Parse("B57CB490-1556-4F06-AA26-96451533A9B8"), tracker.DatasetId);
            Assert.AreEqual(TimeSpan.FromSeconds(10), tracker.Timeout);
        }

        private static void FromConfigAssertion(IServiceCollection serviceCollection)
        {
            ServiceProvider provider = serviceCollection.BuildServiceProvider();

            var tracker = provider.GetService<ITracker>();
            Assert.IsNotNull(tracker);
            Assert.IsNotNull(provider.GetService<IRecommender>());
            Assert.IsNotNull(provider.GetService<ISearcher>());

            Assert.AreEqual(Guid.Parse("6D9361AA-A23D-4BF2-A818-5ABA792E2102"), tracker.DatasetId);
            Assert.AreEqual(TimeSpan.FromSeconds(3), tracker.Timeout);
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