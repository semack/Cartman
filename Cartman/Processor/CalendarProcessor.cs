using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cartman.Configuration;
using Cartman.Constants;
using HtmlAgilityPack;
using Ical.Net;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cartman.Processor
{

    public class CalendarProcessor
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;

        public CalendarProcessor(ILogger<CalendarProcessor> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _appSettings = settings.Value;
        }

        public async Task StartAsync()
        {

            var calendarItems = await FetchCalendarItemsAsync();

            IDateTime today = new CalDateTime(DateTime.Today.AddDays(-2));

            foreach (var calItem in calendarItems)
            {
                var events = calItem.Calendar.Events
                        .Where(c => c.DtStart.GreaterThan(today) && c.DtStart.LessThan(today.AddDays(2)));

                foreach (var item in events)
                {

                    var message = await ParseTemplateAsync(item.Summary,
                        item.Description.Replace("\n\n\n\n", ". "),
                        item.Url.ToString(),
                        calItem.Url);

                    await CallWebHookAsync(message);
                }
            }
        }

        private async Task<IEnumerable<CalendarItem>> FetchCalendarItemsAsync()
        {
            var result = new List<CalendarItem>();
            _appSettings.CalendarSources.ForEach(url =>
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var response = client.GetStringAsync(url).Result;
                        var item = new CalendarItem
                        {
                            Calendar = Calendar.Load(response),
                            Url = url
                        };
                        result.Add(item);
                    }
                    catch(HttpRequestException)
                    {
                    }
                }
            });
            return await Task.FromResult(result);
        }



        private async Task CallWebHookAsync(string message)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(_appSettings.WebHookUrl, new StringContent(message, Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"WebHook call failed. Status code {response.StatusCode}");
            }
        }



        private async Task<string> ParseTemplateAsync(string name, string description, string url, string calendarDownloadUrl)
        {
            var templateFile = _appSettings.DataTemplate;

            var info = new FileInfo(templateFile);
            if (AppContext.BaseDirectory.Equals($"{info.DirectoryName}/"))
                templateFile = $"{ AppContext.BaseDirectory}Templates/{templateFile}";

            if (!File.Exists(templateFile))
                throw new FileNotFoundException("Cannot find template file", templateFile);

            var template = await File.ReadAllTextAsync(templateFile);

            var imageUrl = await GetImageUrlAsync(url);

            if (string.IsNullOrWhiteSpace(imageUrl))
                imageUrl = _appSettings.DefaultImage;

            var result = template
                .Replace(MacroVariables.Name, name)
                .Replace(MacroVariables.Description, description)
                .Replace(MacroVariables.Url, url)
                .Replace(MacroVariables.CalendarDownloadUrl, calendarDownloadUrl)
                .Replace(MacroVariables.ImageUrl, imageUrl);

            _logger.LogDebug(result);

            return result;
        }

        private async Task<string> GetImageUrlAsync(string url)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(url);


            string link = (from x in document.DocumentNode.SelectNodes("/html/head").Descendants()
                           where x.Name == "meta"
                           && x.Attributes["property"] != null
                           && x.Attributes["property"].Value == "og:image"
                           && x.Attributes["content"] != null
                           select x.Attributes["content"].Value).FirstOrDefault();

            return await Task.FromResult(link);
        }
    }
}
