# Quietus

This app accepts a file containing Company + RSS feed url tuples as well as an inactivity period specified in days. The given company feeds will be checked for recent news feed items within the specified period. This app will generate a report of all the companies that have been inactive for the specified number of days.

### ASSUMPTIONS
1. Each company will have <b>ONE</b> and <b>ONLY ONE</b> RSS feed to be checked.
2. All the feeds will either use the property <b>LastUpdatedTime</b> or <b>PublishDate</b> to reflect the date and time that each news item was published. I have encountered feeds that use one of them but not the other, so this application will support the feed as long as one of those properties is used. 
3. The given number of days of inactivity to check for is interpreted as the <b>LAST</b> X consecutive days leading up to right now. If no news items have been posted within the last X days, the company will be considered inactive.
4. The C# Tuple type was not a requirement. I considered implementing this with the use of Tuple, but opted instead to create the Company class with Name, RssFeedUrl, and InactiveDays properties.
5. That a command line application is acceptable for this task, and that it will be run manually

### INSTRUCTIONS
1. Locate an executable Quietus.exe located in the RELEASE folder included in the repo.
2. Call the app via command line:
```
Quietus.exe C:/full/path/to/input/file <number of days of inactivity>
```
3. You may generate your own input file in the following format:
```
CompanyName, http://full.path.to.rss.feed
CompanyName2, http://full.path.to.another.rss.feed
```
4. Or, you can make use of the example file included in the APP_DATA folder. This file contains 20 companies with working RSS urls.

### KNOWN AREAS FOR IMPROVEMENT
1. Better test coverage. I could improve the test coverage on some of the methods that actually hit the RSS urls. I have basic coverage, but in the interest of time I cut that short a bit.
2. Performance. Part of this may be the < 1 mbps connection I'm working with right now, but the application runs slower than I'd expect. One possible way to improve this might be multithreading the calls out to the RSS feeds.
3. I haven't worked with C# in 10+ years, but I wanted to demonstrate that I can do it. If my opting to use C# over a language I'm more familiar with ultimately ends up costing me, I can live with that choice. I wanted the challenge and I'm happy with that decision.