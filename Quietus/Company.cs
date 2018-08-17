using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quietus
{
    public class Company
    {
        public string Name { get; set; }
        public string RssFeedUrl { get; set; }
        public int InactiveDays { get; set; }

        public Company(string name, string rssFeedUrl)
        {
            this.Name = name;
            this.RssFeedUrl = rssFeedUrl;
        }

        public Company(string csvNameFeedTuple)
        {
            string[] pieces = csvNameFeedTuple.Split(',');

            this.Name = pieces[0];
            this.RssFeedUrl = pieces[1].Trim();
        }
    }
}
