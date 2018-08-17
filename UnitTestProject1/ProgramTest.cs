using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quietus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Syndication;

namespace QuietusTest
{
    [TestClass]
    public class ProgramTest
    {
        [TestMethod]
        public void TestGetDaysOfInactivity()
        {
            int daysOld = 7;
            DateTimeOffset lastUpdate = DateTimeOffset.Now.AddDays(daysOld * -1);

            Assert.AreEqual(daysOld, Program.GetDaysOfInactivity(lastUpdate));
        }

        [TestMethod]
        public void TestIsDefault_WhenIsDefault()
        {
            DateTimeOffset defaultDate = default(DateTimeOffset);
            Assert.IsTrue(Program.IsDefault<DateTimeOffset>(defaultDate));
        }

        [TestMethod]
        public void TestIsDefault_WhenIsNotDefault()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            Assert.IsFalse(Program.IsDefault<DateTimeOffset>(now));
        }

        [TestMethod]
        public void TestGetLastPublishDate_withPublishDate()
        {
            SyndicationContent content = SyndicationContent.CreatePlaintextContent("Test News");
            System.Uri uri = new System.Uri("https://test.rss.feed");

            DateTimeOffset publishDate = DateTimeOffset.Now.AddDays(-7);
            DateTimeOffset lastUpdatedTime = default(DateTimeOffset);

            SyndicationItem item = new SyndicationItem("Test Title", content, uri, "1", lastUpdatedTime);
            item.PublishDate = publishDate;

            Assert.AreEqual(publishDate, Program.GetLastPublishDate(item));
        }

        [TestMethod]
        public void TestGetLastPublishDate_withLastUpdatedTime()
        {
            SyndicationContent content = SyndicationContent.CreatePlaintextContent("Test News");
            System.Uri uri = new System.Uri("https://test.rss.feed");

            DateTimeOffset publishDate = default(DateTimeOffset);
            DateTimeOffset lastUpdatedTime = DateTimeOffset.Now.AddDays(-7);

            SyndicationItem item = new SyndicationItem("Test Title", content, uri, "1", lastUpdatedTime);
            item.PublishDate = publishDate;

            Assert.AreEqual(lastUpdatedTime, Program.GetLastPublishDate(item));
        }

        [TestMethod]
        public void TestGetLastPublishDate_withNoDates()
        {
            SyndicationContent content = SyndicationContent.CreatePlaintextContent("Test News");
            System.Uri uri = new System.Uri("https://test.rss.feed");

            DateTimeOffset publishDate = default(DateTimeOffset);
            DateTimeOffset lastUpdatedTime = default(DateTimeOffset);

            SyndicationItem item = new SyndicationItem("Test Title", content, uri, "1", lastUpdatedTime);
            item.PublishDate = publishDate;

            DateTimeOffset? result = Program.GetLastPublishDate(item);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetWebClient()
        {
            WebClient client = Program.GetWebClient();
            string userAgent = client.Headers.Get("user-agent");

            Assert.AreEqual(Program.WEB_CLIENT_USER_AGENT, userAgent);
        }

        [TestMethod]
        [ExpectedException(typeof(WebException))]
        public void TestGetFeedFromUrl_badUrlThrowsWebException()
        {
            Program.GetFeedFromUrl("test.me");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestGetFeedFromUrl_nullUrlThrowsArgumentNullException()
        {
            Program.GetFeedFromUrl(null);
        }

        [TestMethod]
        public void TestGetCompanies()
        {
            List<string> expectedCompanyInfo = new List<string>(new string[] 
            {
                "TestCompany1, http://test.company1.feed.rss",
                "TestCompany2, http://test.company2.feed.rss",
                "TestCompany3, http://test.company3.feed.rss",
                "TestCompany4, http://test.company4.feed.rss",
                "TestCompany5, http://test.company5.feed.rss"
            });

            var safeBasePath = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var fullPath = Path.Combine(safeBasePath, "Quietus");
            
            Directory.CreateDirectory(fullPath);

            string testFile = fullPath + "/test_companies.txt";

            using (StreamWriter writer = new StreamWriter(testFile))
            {
                foreach (string line in expectedCompanyInfo)
                {
                    writer.WriteLine(line);
                }
            }

            List<Company> companies = Program.GetCompanies(testFile);

            List<string> actualCompanyInfo = new List<string>();
            foreach (Company company in companies)
            {
                actualCompanyInfo.Add(company.Name + ", " + company.RssFeedUrl);
            }

            // Clean up our test file and directory.
            File.Delete(testFile);
            Directory.Delete(fullPath);

            CollectionAssert.AreEqual(expectedCompanyInfo, actualCompanyInfo);
        }
    }
}
