using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ShitpostBot.Tests
{
    [TestFixture]
    public class Tests
    {
        protected IConfiguration Configuration { get; private set; }
        protected IServiceProvider ServiceProvider { get; private set; }
        
        public Tests()
        {
            var configBuilder = new ConfigurationBuilder();
            // configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            Configuration = configBuilder.Build();
            var serviceCatalog = new ServiceCollection();
            serviceCatalog.AddLogging();
            // serviceCatalog.AddShitpostBot(Configuration);
            serviceCatalog.AddSingleton(Configuration);
            ServiceProvider = serviceCatalog.BuildServiceProvider();
        }

        [Test]
        public async Task Test1()
        {
            Assert.Pass();
        }
    }
}