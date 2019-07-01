using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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

        public async Task StartAsync(DateTime eventDate)
        {
            var message = await GetMessageAsync(eventDate);
            if (message != null)
            {
                await SendMessageAsync(message);
                _logger.LogInformation(
                    $"A message is containing {message.Attachments.Count} event(s) for {eventDate} has been sent successfully.");

            }
            else
                _logger.LogInformation($"Nothing to do for {eventDate}. Exiting...");
        }

        private async Task<RocketMessage> GetMessageAsync(DateTime eventDate)
        {
            var calendars = await FetchCalendarsAsync();

            IDateTime date = new CalDateTime(eventDate);

            var events = calendars.SelectMany(x => x.Events)
                .Where(c => c.DtStart.Equals(date))
                .ToList();

            if (!events.Any())
                return null;

            var message = new RocketMessage
            {
                UserName = _appSettings.UserName,
                IconUrl = _appSettings.IconUrl,
                Text = _appSettings.Text
                    .Replace(MacroVariables.Date, eventDate.Date.ToString("D"))
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
                    Text = Regex.Replace(item.Description, "\n{2,}", "\n")
                        .Replace(_appSettings.SignatureForRemoval, string.Empty)
                        .Trim(),
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

            return message;
        }

        private async Task<CalendarCollection> FetchCalendarsAsync()
        {
            var result = new CalendarCollection();

            if (!_appSettings.CalendarSources.Any())
                return await Task.FromResult(result);

            _appSettings.CalendarSources.ForEach(url =>
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = client.GetStringAsync(url).Result;

                        var calendar = Calendar.Load(response);
                        calendar.Properties.Add(new CalendarProperty("Url", url));

                        result.Add(calendar);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Can not retrieve calendar data for Url: {url}", ex);
                }
            });

            return await Task.FromResult(result);
        }

        private async Task SendMessageAsync(RocketMessage message)
        {
            var content = JsonConvert.SerializeObject(message);
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(_appSettings.WebHookUrl,
                        new StringContent(content, Encoding.UTF8, "application/json"));

                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"WebHook call failed. Status code {response.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Cannot call web hook", ex);
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