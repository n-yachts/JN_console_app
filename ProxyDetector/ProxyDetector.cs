using System;
using System.Net.Http;
using System.Threading.Tasks;

class ProxyDetector
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: ProxyDetector <IP/hostname>");
            return;
        }

        string target = args[0];

        // Seznam služeb pro detekci VPN/proxy
        string[] detectionServices = {
            $"http://ip-api.com/json/{target}",
            $"https://ipinfo.io/{target}/json"
        };

        foreach (string service in detectionServices)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string json = await client.GetStringAsync(service);
                    Console.WriteLine($"Service: {service}");
                    Console.WriteLine($"Data: {json}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba u {service}: {ex.Message}");
            }
        }
    }
}