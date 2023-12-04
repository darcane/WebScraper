namespace WebScraper.Scraper
{
    public interface IScraper
    {
        Task ScrapeWebsite(string url);
    }
}
