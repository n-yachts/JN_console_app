using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class SimpleWebCrawler
{
    static HashSet<string> visitedUrls = new HashSet<string>();
    static HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: SimpleWebCrawler <start_url>");
            return;
        }

        string startUrl = args[0];
        await Crawl(startUrl);
    }

    static async Task Crawl(string url)
    {
        if (visitedUrls.Contains(url))
            return;

        visitedUrls.Add(url);
        Console.WriteLine($"Navštíveno: {url}");

        try
        {
            string html = await client.GetStringAsync(url);
            var links = ExtractLinks(html, url);

            foreach (string link in links)
            {
                await Crawl(link);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při načítání {url}: {ex.Message}");
        }
    }

    static List<string> ExtractLinks(string html, string baseUrl)
    {
        var links = new List<string>();
        var regex = new Regex(@"<a\s+(?:[^>]*?\s+)?href=""([^""]*)""", RegexOptions.IgnoreCase);
        MatchCollection matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string link = match.Groups[1].Value;
            if (link.StartsWith("http"))
            {
                links.Add(link);
            }
            else if (link.StartsWith("/"))
            {
                Uri baseUri = new Uri(baseUrl);
                links.Add(new Uri(baseUri, link).AbsoluteUri);
            }
        }

        return links;
    }
}