using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Quietus
{
    class Program
    {
        public static int GetDaysOfInactivity(DateTimeOffset lastUpdate)
        {
            TimeSpan timeSpan = DateTimeOffset.Now - lastUpdate;
            return timeSpan.Days;
        }

        public static DateTimeOffset? GetLastPublishDate(SyndicationItem item)
        {
            DateTimeOffset? missingDate = new DateTimeOffset(0001, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset? lastPublishDate = item.PublishDate == missingDate ? item.LastUpdatedTime : item.PublishDate;
            return lastPublishDate != missingDate ? lastPublishDate : null;
        }

        static void Main(string[] args)
        {
            // samsung 403 forbidden
            // no lastUpdatedTime
            // uses publishDate
            // String url = "http://news.samsung.com/global/feed";

            // walmart 
            // no lastUpdatedTime
            // uses publishDate
            // String url = "https://corporate.walmart.com/rss?feedName=allnews";

            // apple
            // no publishDate
            // uses lastUpdatedTime
            String url = "https://www.apple.com/newsroom/rss-feed.rss";

            // oracle no lastUpdatedTime provided
            // String url = "https://www.oracle.com/corporate/press/rss/rss-pr.xml";

            // microsoft investor relations
            // System.Xml.XmlException: 'For security reasons DTD is prohibited in this XML document. To enable DTD processing set the DtdProcessing property on XmlReaderSettings to Parse and pass the settings into XmlReader.Create method.'
            // String url = "https://www.microsoft.com/en-us/Investor/rss.aspx";


            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Quietus/1.0");

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.MaxCharactersFromEntities = 1024;

            XmlReader reader = XmlReader.Create(webClient.OpenRead(url), settings);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();

            SyndicationItem mostRecentItem = feed.Items.First();
            DateTimeOffset? publishDate = GetLastPublishDate(mostRecentItem);

            if (publishDate.HasValue)
            {
                int daysOld = GetDaysOfInactivity(publishDate.Value);
                Console.WriteLine("publish: " + publishDate.ToString() + " (" + daysOld + " days old)");
            }

            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
