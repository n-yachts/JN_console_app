using System;  // Základní jmenný prostor pro vstup/výstup a základní třídy
using System.Net.Http;  // Prostor pro HTTP komunikaci (HttpClient)
using System.Threading.Tasks;  // Podpora asynchronního programování

class HTTPChecker  // Hlavní třída programu
{
    static async Task Main(string[] args)  // Asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: HTTPChecker <URL>");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        string url = args[0];  // Získání URL z prvního argumentu

        // Automatické doplnění schématu pokud chybí
        if (!url.StartsWith("http"))
            url = "http://" + url;

        // Vytvoření HTTP klienta s using pro automatické uvolnění prostředků
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Asynchronní odeslání HTTP požadavku
                HttpResponseMessage response = await client.GetAsync(url);

                // Výpis HTTP stavového kódu
                Console.WriteLine($"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");

                // Pokus o čtení hlavičky Server z odpovědi
                Console.WriteLine($"Server: {response.Headers.Server}");
            }
            catch (Exception ex)  // Zachycení výjimek (např. síťové chyby)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }
    }
}

/*
Asynchronní metoda Main - async Task umožňuje použití await pro neblokující HTTP volání
URL parsování - Program automaticky doplní http:// pokud schéma chybí
Using statement - Zajišťuje správné uvolnění HttpClient po použití
Blok try-catch - Zachytává chyby při síťové komunikaci
HTTP hlavičky - Čte stavový kód a informaci o serveru z odpovědi
*/