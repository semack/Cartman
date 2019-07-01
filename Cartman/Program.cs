using System;
using System.Reflection;
using System.Threading.Tasks;
using Cartman.Configuration;
using Cartman.Processor;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cartman
{
    internal class Options
    {
        [Option('d', "date",
            SetName = "date",
            HelpText = "Specifies a checking date for events in the calendar.")]
        public DateTime? EventDate { get; set; }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(async opts => await RunOptionsAndReturnExitCodeAsync(opts));

            await Task.Yield();
        }

        private static async Task RunOptionsAndReturnExitCodeAsync(Options opts)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var processor = serviceProvider.GetRequiredService<CalendarProcessor>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            var eventDate = DateTime.UtcNow.Date;
            if (opts.EventDate.HasValue) eventDate = opts.EventDate.Value.Date;

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
            logger.LogInformation($"CARTMAN version {version}. (c) Copyright 2019 ONLINICO.");

            await processor.StartAsync(eventDate);
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // build config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appSettings.json")
                .Build();

            // setup config
            services.AddOptions();

            services.Configure<AppSettings>(configuration.GetSection("Settings"));

            services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddConfiguration(configuration.GetSection("Logging"));
                configure.AddConsole();
            });

            services.AddSingleton<CalendarProcessor>();
        }
    }
}