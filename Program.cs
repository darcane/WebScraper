// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WebScraper;
using WebScraper.Scraper;

try
{
    var serviceProvider = new ServiceCollection()
        .AddLogging(c =>
        {
            c.SetMinimumLevel(LogLevel.Debug);
            c.AddSimpleConsole(c =>
            {
                c.SingleLine = true;
                c.IncludeScopes = false;
                c.ColorBehavior = LoggerColorBehavior.Enabled;
                c.TimestampFormat = "HH:mm.ss";
            });
        })
        .AddSingleton(typeof(HttpClient))
        .AddSingleton<IScraper, Scraper>()
        .BuildServiceProvider();
    
    var logger = serviceProvider.GetService<ILogger<Program>>();
    logger.LogDebug("Starting application");

    var scraper = serviceProvider.GetService<IScraper>();

    await scraper.ScrapeWebsite(Constants.BaseUrl);

}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

