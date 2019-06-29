using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cartman.Configuration;
using Cartman.Constants;
using Cartman.Processor.Models;
using HtmlAgilityPack;
using Ical.Net;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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

            var calendars = await FetchCalendarItemsAsync();

            //IDateTime today = new CalDateTime(DateTime.Today);

            IDateTime today = new CalDateTime(DateTime.Today.AddDays(-3));

            //IDateTime today = new CalDateTime(new DateTime(2019,10,13));

            var events = calendars.SelectMany(x => x.Events)
                .Where(c => c.DtStart.GreaterThan(today) && c.DtStart.LessThan(today.AddDays(2)));

            if (!events.Any())
                return;

            var message = new RocketMessage
            {
                UserName = _appSettings.UserName,
                IconUrl = _appSettings.IconUrl,
                Text = _appSettings.Text
                .Replace(MacroVariables.Date, DateTime.Today.AddDays(1).ToString("D"))
            };

            var plural = string.Empty;

            if (events.Count() > 1)
                plural = "s";

            message.Text = message.Text.Replace(MacroVariables.Plural, plural);

            foreach (var item in events)
            {
                var url = item.Url.ToString();
                var imageUrl = await GetImageUrlAsync(url);

                if (string.IsNullOrWhiteSpace(imageUrl))
                    imageUrl = _appSettings.DefaultImage;

                var attachment = new RocketAttachment()
                {
                    Title = item.Summary,
                    TitleLink = url,
                    MessageLink = url,
                    ImageUrl = imageUrl,
                    Text = item.Description,
                    Fields = new List<RocketField> {
                            new RocketField{
                                Title = item.Summary,
                                Value = $"[Read]({url}) more about the holiday.",
                            },
                            new RocketField{
                                Title = item.Calendar.Properties["X-WR-CALNAME"].Value.ToString(),
                                Value = $"[Download]({item.Calendar.Properties["Url"].Value.ToString()}) the calendar."
                            }
                        }
                };

                message.Attachments.Add(attachment);

                var content = JsonConvert.SerializeObject(message);

                await CallWebHookAsync(content);
            }
        }

        private async Task<CalendarCollection> FetchCalendarItemsAsync()
        {

            CalendarCollection result = new CalendarCollection();

            _appSettings.CalendarSources.ForEach(url =>
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        var response = client.GetStringAsync(url).Result;

                        var calendar = Calendar.Load(response);
                        calendar.Properties.Add(new CalendarProperty("Url", url));

                        result.Add(calendar);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex.Message);
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
