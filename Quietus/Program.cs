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
        const int DEFAULT_INACTIVITY_PERIOD_DAYS = 7;

        /// <summary>
        /// Accepts a DateTimeOffset representing the last date an item was published
        /// to the Company's news feed and returns the number of days that have elapsed
        /// since that date.
        /// </summary>
        /// <param name="lastUpdate">The DateTimeOffset of the most recent news item.</param>
        /// <returns>The number of days of feed inactivity.</returns>
        public static int GetDaysOfInactivity(DateTimeOffset lastUpdate)
        {
            TimeSpan timeSpan = DateTimeOffset.Now - lastUpdate;
            return timeSpan.Days;
        }

        /// <summary>
        /// Accepts a SyndicationItem of the most recent RSS feed entry and returns a
        /// DateTimeOffset for the publish date of the entry or null if the date can't be
        /// found. Some RSS feeds use SyndicationItem.PublishDate for this information, while
        /// others may instead use SyndicationItem.LastUpdatedTime. This method checks both.
        /// </summary>
        /// <param name="item">The SyndicationItem for the most recent RSS feed entry.</param>
        /// <returns>DateTimeOffset for the most recent RSS entry or null if undetected.</returns>
        public static DateTimeOffset? GetLastPublishDate(SyndicationItem item)
        {
            DateTimeOffset? missingDate = new DateTimeOffset(0001, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
            DateTimeOffset? lastPublishDate = item.PublishDate == missingDate ? item.LastUpdatedTime : item.PublishDate;
            return lastPublishDate != missingDate ? lastPublishDate : null;
        }

        /// <summary>
        /// Returns a WebClient with a user-agent header to identify the app. 
        /// This is a requirement in order to access some RSS feeds.
        /// </summary>
        /// <returns>WebClient with Quietus identified as the user-agent.</returns>
        public static WebClient GetWebClient()
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Quietus/1.0");
            return webClient;
        }

        /// <summary>
        /// Accepts a string url for the Company RSS feed and returns a SyndicationItem
        /// of the most recent post found.
        /// </summary>
        /// <param name="url">The full url for the Company RSS feed.</param>
        /// <returns>SyndicationItem of the most recent entry from the RSS feed.</returns>
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

        /// <summary>
        /// Accepts a string representation of the full file path where the input file
        /// may be found. The file format accepted is "<CompanyName>, <RSS Feed Url>"
        /// with one Company/RSS feed tuple per line.
        /// </summary>
        /// <param name="filePath">The string of the full path to the input file.</param>
        /// <returns>A List of Company objects generated from the input file contents.</returns>
        public static List<Company> GetCompanies(string filePath)
        {
            List<Company> companies = new List<Company>();
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    string[] pieces = line.Split(',');
                    companies.Add(new Company(line));
                }
            }
            return companies;
        }

        /// <summary>
        /// Accepts a List of all Companies and an integer for the number of days of inactivity 
        /// that are required for a Company to be considered inactive, and returns a List of
        /// the inactive Companies with the number of InactiveDays populated.
        /// </summary>
        /// <param name="allCompanies">The List of all Companies pulled from the input file.</param>
        /// <param name="inactivityPeriod">The number of days of inactivity required to be inactive.</param>
        /// <returns>A List of Company objects of the inactive Companies.</returns>
        public static List<Company> GetInactiveCompanies(List<Company> allCompanies, int inactivityPeriod)
        {
            List<Company> inactiveCompanies = new List<Company>();

            foreach (Company company in allCompanies)
            {
                SyndicationItem mostRecentItem = GetMostRecentItem(company.RssFeedUrl);
                DateTimeOffset? publishDate = GetLastPublishDate(mostRecentItem);

                if (publishDate.HasValue)
                {
                    company.InactiveDays = GetDaysOfInactivity(publishDate.Value);
                    if (company.InactiveDays >= inactivityPeriod)
                    {
                        inactiveCompanies.Add(company);
                    }
                }
            }
            return inactiveCompanies;
        }

        /// <summary>
        /// Accepts a List of inactive Companies and generates a string represenation of
        /// the inactivity report, detailing the Company name and the number of days the Company
        /// news feed has been inactive.
        /// </summary>
        /// <param name="inactiveCompanies">List of inactive Company objects.</param>
        /// <returns>A string report of Company inactivity.</returns>
        public static string GetInactivityReport(List<Company> inactiveCompanies)
        {
            string report = System.Environment.NewLine;

            foreach (Company company in inactiveCompanies)
            {
                report += (company.Name + " feed has been inactive for " + 
                    company.InactiveDays + " day(s)." + System.Environment.NewLine);
            }

            return report;
        }

        static void Main(string[] args)
        {
            string filePath;
            int inactivityPeriod;

            if (args.Length > 0)
            {
                filePath = args[0];
            }
            else
            {
                //Console.WriteLine("Please provide the full path to the input file, e.g. 'C:/Folder/Filename.txt'");
                //filePath = Console.ReadLine();
                filePath = "C:/Users/akay/Documents/company_feeds.txt";
            }

            if (args.Length < 2 || !Int32.TryParse(args[1], out inactivityPeriod))
            {
                Console.WriteLine("Please provide the duration of the inactivity period in days.");
                if (!Int32.TryParse(Console.ReadLine(), out inactivityPeriod))
                {
                    // Cannot parse provided value, use the default period instead.
                    Console.WriteLine("Unable to parse user input, using default period of " + DEFAULT_INACTIVITY_PERIOD_DAYS + " days.");
                    inactivityPeriod = DEFAULT_INACTIVITY_PERIOD_DAYS;
                }
            }

            List<Company> allCompanies = GetCompanies(filePath);
            List<Company> inactiveCompanies = GetInactiveCompanies(allCompanies, inactivityPeriod);
            string report = GetInactivityReport(inactiveCompanies);

            Console.WriteLine(report);

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
