using System;
using System.Collections.Generic;
using System.IO;
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

        public static WebClient GetWebClient()
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Quietus/1.0");
            return webClient;
        }

        public static SyndicationItem GetMostRecentItem(string url)
        {
            WebClient webClient = GetWebClient();

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.MaxCharactersFromEntities = 1024;

            XmlReader reader = XmlReader.Create(webClient.OpenRead(url), settings);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            reader.Close();

            SyndicationItem mostRecentItem = feed.Items.First();
            return mostRecentItem;
        }

        public static Dictionary<string, string> GetCompanyFeedMap(string filePath)
        {
            Dictionary<string, string> companyFeedMap = new Dictionary<string, string>();
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] pieces = line.Split(' ');
                    companyFeedMap.Add(pieces[0], pieces[1]);
                }
            }
            return companyFeedMap;
        }

        static void Main(string[] args)
        {

            foreach (KeyValuePair<string, string> keyValue in GetCompanyFeedMap("C:/Users/akay/Documents/company_feeds.txt"))
            {
                string company = keyValue.Key;
                string feedUrl = keyValue.Value;

                Console.WriteLine(company + ":");

                SyndicationItem mostRecentItem = GetMostRecentItem(feedUrl);
                DateTimeOffset? publishDate = GetLastPublishDate(mostRecentItem);

                if (publishDate.HasValue)
                {
                    int daysOld = GetDaysOfInactivity(publishDate.Value);
                    Console.WriteLine("publish: " + publishDate.ToString() + " (" + daysOld + " days old)");
                }

            }

            Console.ReadLine();
        }
    }
}
