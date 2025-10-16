using System;
using System.Net.Http;
using System.Threading.Tasks;

class HeaderAnalyzer
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: HeaderAnalyzer <URL>");
            return;
        }

        string url = args[0];

        using (HttpClient client = new HttpClient())
        {
            // Nastavení user-agent aby vypadal jako reálný prohlížeč
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            HttpResponseMessage response = await client.GetAsync(url);

            Console.WriteLine($"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine("\nHlavičky:");

            foreach (var header in response.Headers)
            {
                Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
        }
    }
}