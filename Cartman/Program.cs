using System;
using System.Threading.Tasks;
using Cartman.Configuration;
using Cartman.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cartman
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var processor = serviceProvider.GetRequiredService<CalendarProcessor>();
            await processor.StartAsync();

            await Task.CompletedTask;
        }


        private static void ConfigureServices(ServiceCollection services)
        {
            // build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appSettings.json", false, true)
                .Build();

            // setup config
            services.AddOptions();

            services.Configure<AppSettings>(configuration.GetSection("Settings"));

            services.AddLogging(configure => configure.AddConsole());

            services.AddSingleton<CalendarProcessor>();
        }
    }
}