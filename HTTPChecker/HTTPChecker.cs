using System;
using System.Net.Http;
using System.Threading.Tasks;

class HTTPChecker
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: HTTPChecker <URL>");
            return;
        }

        string url = args[0];
        if (!url.StartsWith("http"))
            url = "http://" + url;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                Console.WriteLine($"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");
                Console.WriteLine($"Server: {response.Headers.Server}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }
    }
}