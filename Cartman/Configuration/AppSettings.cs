using System;
using System.Collections.Generic;

namespace Cartman.Configuration
{

    //curl -X POST -H 'Content-Type: application/json' --data '{"username":"Cartman","icon_url":"https://files.gamebanana.com/img/ico/sprays/_cartman.png","text":"Example message","attachments":[{"title":"Rocket.Chat","title_link":"https://rocket.chat","text":"Rocket.Chat, the best open source chat","image_url":"/images/integration-attachment-example.png","color":"#764FA5"}]}' https://chat.onlini.co/hooks/ovSkdxBiExtJkfK7X/4S6JP9yikgyXT446zfx5Z84JBEfgMBaFafJswrS8RcYoozT6
    public class AppSettings
    {
        public List<string> CalendarSources { get; set; }
        public string WebHookUrl { get; set; }
        public string DataTemplate { get; set; }
        public string DefaultImage { get; set; }
        public string UserName { get; set; }
        public string IconUrl { get; set; }
        public string Text { get; set; }

    }
}
