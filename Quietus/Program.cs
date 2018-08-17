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
    public class Program
    {
        public const string WEB_CLIENT_USER_AGENT = "Quietus/1.0";
        public const int DEFAULT_INACTIVITY_PERIOD_DAYS = 7;

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
        /// Accepts a value and compares it against the default value for that object type
        /// and returns true if the values are equal.
        /// </summary>
        /// <param name="value">The value to check against the default value.</param>
        /// <returns>True, if the value is the default value and false if not.</returns>
        public static bool IsDefault<T>(T value)
        {
            return EqualityComparer<T>.Default.Equals(value, default(T));
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
            bool defaultPublish = IsDefault<DateTimeOffset>(item.PublishDate);
            DateTimeOffset lastPublish = defaultPublish ? item.LastUpdatedTime : item.PublishDate;

            bool defaultLastPublish = IsDefault<DateTimeOffset>(lastPublish);

            return defaultLastPublish ? (DateTimeOffset?)null : lastPublish;
        }

        /// <summary>
        /// Returns a WebClient with a user-agent header to identify the app. 
        /// This is a requirement in order to access some RSS feeds.
        /// </summary>
        /// <returns>WebClient with Quietus identified as the user-agent.</returns>
        public static WebClient GetWebClient()
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", WEB_CLIENT_USER_AGENT);
            return webClient;
        }

        /// <summary>
        /// Accepts a string url for the Company RSS feed and returns a SyndicationFeed
        /// </summary>
        /// <param name="url">The full url for the Company RSS feed.</param>
        /// <returns>SyndicationFeed for the RSS feed.</returns>
        /// <exception cref="WebException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static SyndicationFeed GetFeedFromUrl(string url)
        {
            try
            {
                using (WebClient webClient = GetWebClient())
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.DtdProcessing = DtdProcessing.Parse;
                    settings.MaxCharactersFromEntities = 1024;

                    using (XmlReader reader = XmlReader.Create(webClient.OpenRead(url), settings))
                    {
                        SyndicationFeed feed = SyndicationFeed.Load(reader);
                        return feed;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Accepts a string representation of the full file path where the input file
        /// may be found. The file format accepted is [CompanyName], [RSS Feed Url]
        /// with one Company/RSS feed tuple per line.
        /// </summary>
        /// <param name="filePath">The string of the full path to the input file.</param>
        /// <returns>A List of Company objects generated from the input file contents.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        public static List<Company> GetCompanies(string filePath)
        {
            try
            {
                List<Company> companies = new List<Company>();
                using (StreamReader streamReader = new StreamReader(filePath))
                {
                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        companies.Add(new Company(line));
                    }
                }
                return companies;

            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Accepts a List of all Companies and an integer for the number of days of inactivity 
        /// that are required for a Company to be considered inactive, and returns a List of
        /// the inactive Companies with the number of InactiveDays populated.
        /// </summary>
        /// <param name="allCompanies">The List of all Companies pulled from the input file.</param>
        /// <param name="inactivityPeriod">The number of days of inactivity required to be inactive.</param>
        /// <returns>A List of Company objects of the inactive Companies.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="WebException"></exception>
        public static List<Company> GetInactiveCompanies(List<Company> allCompanies, int inactivityPeriod)
        {
            try
            {
                List<Company> inactiveCompanies = new List<Company>();

                foreach (Company company in allCompanies)
                {
                    SyndicationFeed feed = GetFeedFromUrl(company.RssFeedUrl);
                    SyndicationItem mostRecentItem = feed.Items.FirstOrDefault();

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
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Accepts a List of inactive Companies and generates a string represenation of
        /// the inactivity report, detailing the Company name and the number of days the Company
        /// news feed has been inactive.
        /// </summary>
        /// <param name="inactiveCompanies">List of inactive Company objects.</param>
        /// <returns></returns>
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
                Console.WriteLine("Please provide the full path to the input file, e.g. 'C:/Folder/Filename.txt'");
                filePath = Console.ReadLine();
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

            try
            {
                List<Company> allCompanies = GetCompanies(filePath);
                List<Company> inactiveCompanies = GetInactiveCompanies(allCompanies, inactivityPeriod);
                string report = GetInactivityReport(inactiveCompanies);

                Console.WriteLine(report);

                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();

            }
            catch (Exception)
            {
                Console.WriteLine("An error has occurred, please try again.");
            }
        }
    }
}
