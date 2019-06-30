using System;
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
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => RunOptionsAndReturnExitCodeAsync(opts).Wait());
        }

        private static async Task RunOptionsAndReturnExitCodeAsync(Options opts)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var processor = serviceProvider.GetRequiredService<CalendarProcessor>();

            Console.WriteLine(
                "Cartman. Copyright (c) ONLINICO\r\nPlease use --help or -h key for more information.\r\n");

            var eventDate = DateTime.UtcNow.Date;
            if (opts.EventDate.HasValue)
            {
                eventDate = opts.EventDate.Value.Date;
                Console.Write($"Specified date - {eventDate:D}\r\n");
            }
            else
                Console.Write($"No date specified. Using today - {eventDate:D}\r\n");

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
                configure.AddConfiguration(configuration.GetSection("Logging"));
                configure.AddConsole();
            });

            services.AddSingleton<CalendarProcessor>();
        }
    }
}