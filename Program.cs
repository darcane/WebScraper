﻿// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WebScraper;
using WebScraper.Scraper;

try
{
    Console.CursorVisible = false;

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
        //.Configure<HttpClient>(c =>
        //{
        //    c.BaseAddress
        //})
        .AddSingleton<IScraper, Scraper>()
        .BuildServiceProvider();
    
    var logger = serviceProvider.GetService<ILogger<Program>>();
    if (logger == null)
        throw new ArgumentNullException(nameof(logger));

    logger.LogDebug("Starting application");

    var scraper = serviceProvider.GetService<IScraper>();
    if (scraper == null)
        throw new ArgumentNullException(nameof(scraper));

    await scraper.ScrapeWebsite(Helpers.BaseUrl);

}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

