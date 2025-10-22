using System;           // Základní jmenný prostor pro vstup/výstup a základní třídy
using System.Net.Sockets; // Třídy pro práci se síťovými spojeními (TcpClient, NetworkStream)
using System.Text;      // Práce s textovými kodováními (Encoding.ASCII)
using System.Threading.Tasks; // Podpora asynchronního programování (Task, async/await)

class WhoisClient      // Hlavní třída WHOIS klienta
{
    static async Task Main(string[] args)  // Asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů příkazové řádky
        if (args.Length != 1)
        {
            // Výpis správného použití pokud není zadán jeden argument
            Console.WriteLine("Použití: WhoisClient <domain>");
            return;  // Předčasné ukončení programu
        }

        string domain = args[0];  // Uložení prvního argumentu jako doménové jméno
        string whoisServer = "whois.iana.org";  // Primární WHOIS server pro základní informace
        int port = 43;            // Standardní port pro WHOIS protokol

        try  // Ošetření možných chyb spojení a komunikace
        {
            // Vytvoření a automatické uvolnění TCP klienta (using statement)
            using TcpClient client = new TcpClient();

            // Asynchronní připojení k WHOIS serveru na standardním portu
            await client.ConnectAsync(whoisServer, port);

            // Získání síťového proudu pro komunikaci s automatickým uvolněním
            using NetworkStream stream = client.GetStream();

            // Převod domény na bajty s přidáním CR/LF (požadavek WHOIS protokolu)
            byte[] request = Encoding.ASCII.GetBytes(domain + "\r\n");

            // Odeslání požadavku na server asynchronně
            await stream.WriteAsync(request, 0, request.Length);

            // Vyrovnávací paměť pro příjem dat od serveru (4KB bloky)
            byte[] buffer = new byte[4096];
            int bytesRead;        // Skutečný počet přečtených bajtů
            StringBuilder response = new StringBuilder();  // Efektivní skládání řetězce odpovědi

            // Čtení dat po blocích dokud server posílá data (bytesRead > 0)
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                // Převod přijatých bajtů na řetězec a přidání do stavitele
                response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }

            // Výpis hlavičky výsledku
            Console.WriteLine($"WHOIS informace pro {domain}:");
            // Výpis kompletní odpovědi od WHOIS serveru
            Console.WriteLine(response.ToString());
        }
        catch (Exception ex)  // Zachycení všech výjimek (síťové chyby, atd.)
        {
            // Výpis chybové zprávy uživateli
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}

/*
Zkompilujte jako konzolovou aplikaci
Spusťte příkazem: WhoisClient example.com
Program zobrazí registrované informace o doméně

Tento kód demonstruje základní WHOIS dotaz.
Pro kompletní informace je často potřeba následovat přesměrování na koncový WHOIS server uvedený v odpovědi.
*/