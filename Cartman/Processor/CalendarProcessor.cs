using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cartman.Configuration;
using Cartman.Constants;
using Cartman.Models;
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
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;

        public CalendarProcessor(ILogger<CalendarProcessor> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _appSettings = settings.Value;
        }

        public async Task StartAsync()
        {
            var calendars = await FetchCalendarsAsync();

            IDateTime today = new CalDateTime(DateTime.Today);

            var events = calendars.SelectMany(x => x.Events)
                .Where(c => c.DtStart.GreaterThan(today) && c.DtStart.LessThan(today.AddDays(2)))
                .ToList();

            if (!events.Any())
            {
                _logger.LogInformation("Nothing to do.");
                return;
            }

            var message = new RocketMessage
            {
                UserName = _appSettings.UserName,
                IconUrl = _appSettings.IconUrl,
                Text = _appSettings.Text
                    .Replace(MacroVariables.Date, today.AddDays(1).Date.ToString("D"))
            };

            foreach (var item in events)
            {
                var url = item.Url.ToString();
                var imageUrl = await GetImageUrlAsync(url);

                if (string.IsNullOrWhiteSpace(imageUrl))
                    imageUrl = _appSettings.DefaultImage;

                var attachment = new RocketAttachment
                {
                    Title = item.Summary,
                    TitleLink = url,
                    MessageLink = url,
                    ImageUrl = imageUrl,
                    Text = item.Description,
                    Fields = new List<RocketField>
                    {
                        new RocketField
                        {
                            Title = item.Summary,
                            Value = $"[Read]({url}) more about the holiday."
                        },
                        new RocketField
                        {
                            Title = item.Calendar.Properties["X-WR-CALNAME"].Value.ToString(),
                            Value = $"[Download]({item.Calendar.Properties["Url"].Value}) the calendar."
                        }
                    }
                };

                message.Attachments.Add(attachment);
            }

            await SendMessageAsync(message);

            _logger.LogInformation($"A message is containing {events.Count} event(s) has been sent successfully.");
        }

        private async Task<CalendarCollection> FetchCalendarsAsync()
        {
            var result = new CalendarCollection();

            if (!_appSettings.CalendarSources.Any())
                return await Task.FromResult(result);

            using (var client = new HttpClient())
            {
                _appSettings.CalendarSources.ForEach(url =>
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
                        _logger.LogError(ex, $"Can not retrieve calendar data for Url: {url}");
                    }
                });
            }

            return await Task.FromResult(result);
        }

        private async Task SendMessageAsync(RocketMessage message)
        {
            var content = JsonConvert.SerializeObject(message);

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(_appSettings.WebHookUrl,
                    new StringContent(content, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"WebHook call failed. Status code {response.StatusCode}");
            }
        }

        private async Task<string> GetImageUrlAsync(string url)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(url);

            var link = (from x in document.DocumentNode.SelectNodes("/html/head").Descendants()
                where x.Name == "meta"
                      && x.Attributes["property"] != null
                      && x.Attributes["property"].Value == "og:image"
                      && x.Attributes["content"] != null
                select x.Attributes["content"].Value).FirstOrDefault();

            return await Task.FromResult(link);
        }
    }
}