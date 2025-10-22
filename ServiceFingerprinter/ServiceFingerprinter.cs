using System;  // Import základních systémových knihoven
using System.Net.Sockets;  // Import knihovny pro práci s TCP sokety
using System.Text;  // Import knihoven pro práci s textovými encodingy
using System.Threading.Tasks;  // Import knihoven pro asynchronní programování

class ServiceFingerprinter  // Hlavní třída programu
{
    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje přesně 2 argumenty
        if (args.Length != 2)
        {
            // Výpis správného použití při chybném počtu argumentů
            Console.WriteLine("Použití: ServiceFingerprinter <host> <port>");
            return;  // Předčasné ukončení programu
        }

        string host = args[0];  // Uložení prvního argumentu jako hostitele
        int port = int.Parse(args[1]);  // Převedení druhého argumentu na číslo portu

        // Vytvoření TCP klienta s automatickým uklizením (using statement)
        using (TcpClient client = new TcpClient())
        {
            try  // Ošetření možných chyb spojení a komunikace
            {
                // Asynchronní navázání spojení se zadaným hostitelem a portem
                await client.ConnectAsync(host, port);
                // Získání síťového streamu pro čtení a zápis dat
                NetworkStream stream = client.GetStream();

                // Příprava bufferu pro přijetí dat (banneru)
                byte[] buffer = new byte[1024];
                // Asynchronní čtení příchozích dat ze služby
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                // Převod přijatých bajtů na textový řetězec v UTF-8 kódování
                string banner = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Výpis získaného banneru služby
                Console.WriteLine($"Banner služby na {host}:{port}:");
                Console.WriteLine(banner);

                // Kontrola čísel portů typických pro HTTP služby
                if (port == 80 || port == 443 || port == 8080)
                {
                    // Příprava základního HTTP GET požadavku
                    byte[] httpRequest = Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\n\r\n");
                    // Odeslání HTTP požadavku do síťového streamu
                    await stream.WriteAsync(httpRequest, 0, httpRequest.Length);

                    // Čtení HTTP odpovědi od služby
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    // Převod přijaté HTTP odpovědi na text
                    string httpResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Výpis kompletní HTTP odpovědi
                    Console.WriteLine("\nHTTP odpověď:");
                    Console.WriteLine(httpResponse);
                }
            }
            catch (Exception ex)  // Zachycení všech možných výjimek
            {
                // Výpis chybové zprávy uživateli
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }  // Automatické uzavření TCP spojení zde (díky using)
    }
}

/*
Kontrola argumentů: Program vyžaduje dva vstupní parametry - hostitele a port
TCP spojení: Naváže spojení se zadanou službou pomocí TCP protokolu
Banner grabbing: Čeká na úvodní zprávu (banner), kterou mnoho služeb posílá při spojení
HTTP detekce: Pro porty 80/443/8080 automaticky posílá HTTP požadavek
Analýza odpovědi: Zobrazí kompletní odpověď od služby včetně hlaviček
*/