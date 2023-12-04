using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Linq;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;

namespace WebScraper.Scraper
{
    public class Scraper(HttpClient client, ILogger<Scraper> logger): IScraper
    {
        private const string RootFolder = "./root";
        private string BaseOnRoot(string url) => $"{RootFolder}/{url}";

        private static readonly object visitlocker = new object();
        private static readonly object foundlocker = new object();

        private volatile List<string> UrlsVisited = new List<string>();
        private volatile ConcurrentQueue<string> UrlsFound = new ConcurrentQueue<string>();

        private string BaseUrl { get; set; }
        private Uri GetFullUri(Uri relativeUri) => new Uri(new Uri(BaseUrl), relativeUri);

        public async Task ScrapeWebsite(string rootUrl)
        {
            BaseUrl = rootUrl;
            var baseUri = new Uri(BaseUrl);
            logger.LogDebug($"Scraping starting with url: {BaseUrl}");
            Directory.CreateDirectory(RootFolder);

            await DownloadAllImagesOnPage(baseUri);

            var htmlDocument = await GetDocumentFromUrl(baseUri);
            htmlDocument.Save(BaseOnRoot("index.html"));
            UrlsVisited.Add(rootUrl);
            await ProcessAllLinksOnPage(baseUri);

            List<Task> runningTask = new List<Task>();

            int parallelTasks = 8;

            while(UrlsFound.Count > 0 || runningTask.Count > 0)
            {
                while(runningTask.Count < parallelTasks && UrlsFound.TryDequeue(out var currentItem))
                {
                    runningTask.Add(ProcessPage(currentItem));
                }

                var completedTask = await Task.WhenAny(runningTask);

                runningTask.Remove(completedTask);

                logger.LogWarning($"Loop ended");
                logger.LogWarning($"UrlsVisited Count = {UrlsVisited.Count}");
                logger.LogWarning($"UrlsFound Count = {UrlsFound.Count}");
                logger.LogTrace("");
            }

            await Console.Out.WriteLineAsync("Hello");
        }

        public async Task ProcessPage(string url)
        {
            var uri = new Uri(url);
            await ProcessAllLinksOnPage(uri);
            await DownloadAllImagesOnPage(uri);
            var structure = GetFolderStructure(uri);
            var doc = await GetDocumentFromUrl(uri);
            Directory.CreateDirectory(BaseOnRoot(structure.FolderPath));
            lock (visitlocker)
            {
                UrlsVisited.Add(uri.AbsoluteUri);
            }
            doc.Save(BaseOnRoot(uri.LocalPath));
        }

        private async Task<HtmlDocument> GetDocumentFromUrl(Uri url)
        {
            var response = await client.GetAsync(url);
            var pageContent = await response.Content.ReadAsStringAsync();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(pageContent);

            return htmlDocument;
        }

        private PathParts GetFolderStructure(Uri url)
        {
            var parts = url.LocalPath.Split('/');
            var fileName = parts.Last();
            return new PathParts
            {
                FileName = fileName,
                FolderPath = url.LocalPath.Replace(fileName,"")
            };

        }

        private async Task ProcessAllLinksOnPage(Uri url)
        {
            var doc = await GetDocumentFromUrl(url);
            var links = new List<Uri>();

            links.AddRange(GetAllLinksOnPage(doc));
            links.AddRange(GetAllScriptsOnPage(doc));

            foreach(var link in links)
            {
                if (link.IsAbsoluteUri)
                    continue;

                var fullUri = GetFullUri(link).AbsoluteUri;
                if (UrlsVisited.Contains(fullUri))
                    continue;

                lock (foundlocker)
                {
                    UrlsFound.Enqueue(fullUri);
                }
            }
        }

        private List<Uri> GetAllLinksOnPage(HtmlDocument document)
        {
            var links = new List<Uri>();

            var anchors = document.DocumentNode.SelectNodes("//a");

            if (anchors != null && anchors.Count > 0)
            {
                foreach (var a in anchors)
                {
                    var href = a.Attributes["href"];
                    if (href == null)
                        continue;

                    links.Add(new Uri(href.Value, UriKind.RelativeOrAbsolute));
                }
            }
            return links;
        }

        private List<Uri> GetAllScriptsOnPage(HtmlDocument document)
        {
            var links = new List<Uri>();

            var scripts = document.DocumentNode.SelectNodes("//script");
            if (scripts != null && scripts.Count > 0)
            {
                foreach (var script in scripts)
                {
                    var src = script.Attributes["src"];
                    if (src == null)
                        continue;

                    links.Add(new Uri(src.Value, UriKind.RelativeOrAbsolute));
                }
            }
            return links;
        }

        private async Task DownloadAllImagesOnPage(Uri url)
        {
            var doc = await GetDocumentFromUrl(url);
            var images = doc.DocumentNode.SelectNodes("//img");
            if (images == null || images.Count == 0) 
                return;

            var tasks = images.Select(item => Task.Run(async () =>
            {
                var src = item.Attributes["src"];
                if (src == null)
                    return;

                var source = new Uri(src.Value, UriKind.RelativeOrAbsolute);
                if (source.IsAbsoluteUri)
                    return;

                var fullUri = GetFullUri(source);
                await DownloadImage(fullUri);
            })).ToArray();
            Task.WaitAll(tasks);
        }

        private async Task DownloadImage(Uri fullUri)
        {
            var imageData = await client.GetByteArrayAsync(fullUri.AbsoluteUri);

            var structure = GetFolderStructure(fullUri);
            Directory.CreateDirectory(BaseOnRoot(structure.FolderPath));
            await File.WriteAllBytesAsync(BaseOnRoot(fullUri.LocalPath), imageData);
        }
    }

    public struct PathParts
    {
        public string FolderPath { get; set; }
        public string FileName { get; set;}
    }
}
