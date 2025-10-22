using System;  // Importování základních systémových knihoven pro práci s konzolí a výjimkami
using System.Net.Http;  // Importování knihovny pro HTTP komunikaci (HttpClient)
using System.Threading.Tasks;  // Importování knihoven pro asynchronní programování (Task, async/await)

class ProxyDetector  // Hlavní třída programu pro detekci proxy/VPN
{
    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje přesně 1 argument
        if (args.Length != 1)
        {
            // Výpis správného způsobu použití při chybném počtu argumentů
            Console.WriteLine("Použití: ProxyDetector <IP/hostname>");
            return;  // Předčasné ukončení programu
        }

        string target = args[0];  // Uložení prvního argumentu jako cílové IP/hostname

        // Pole URL adres externích služeb pro detekci proxy/VPN
        string[] detectionServices = {
            $"http://ip-api.com/json/{target}",  // Služba 1: IP-API.com
            $"https://ipinfo.io/{target}/json"   // Služba 2: IPinfo.io
        };

        // Cyklus pro zpracování všech detekčních služeb
        foreach (string service in detectionServices)
        {
            try  // Zachycení potenciálních chyb při HTTP požadavku
            {
                using (HttpClient client = new HttpClient())  // Vytvoření HTTP klienta s automatickým uvolněním prostředků
                {
                    // Asynchronní odeslání GET požadavku a načtení odpovědi jako řetězce
                    string json = await client.GetStringAsync(service);

                    // Výpis názvu právě používané služby
                    Console.WriteLine($"Service: {service}");

                    // Výpis nezpracované JSON odpovědi od služby
                    Console.WriteLine($"Data: {json}\n");
                }
            }
            catch (Exception ex)  // Zpracování výjimek (chyb síťové komunikace, atd.)
            {
                // Výpis informací o chybě včetně zdroje selhání
                Console.WriteLine($"Chyba u {service}: {ex.Message}");
            }
        }
    }
}

/*
Kontrola argumentů: Program vyžaduje přesně jeden vstupní parametr (IP adresu nebo hostname). Při nesprávném počtu vypíše návod k použití.
Detekční služby: Používá dvě veřejné API:
 IP-API.com: Poskytuje geolokační a technické informace o IP adrese
 IPinfo.io: Vrací strukturovaná data o IP adrese včetně detekce VPN/proxy
Zpracování služeb:
 Pro každou službu vytvoří nový HTTP klient
 Asynchronně získá JSON odpověď
 Vypíše surová data z API
 Obsluha chyb zachytává problémy s připojením nebo nefunkční služby
Výstup: Program vypisuje nezpracovaná JSON data, která typicky obsahují:
 Zemi a město
 Poskytovatele internetu
 Informace o tom, zda se jedná o hosting/VPN/proxy
*/