using System;  // Importování základních systémových knihoven
using System.Net.Http;  // Importování knihoven pro HTTP komunikaci
using System.Threading.Tasks;  // Importování knihoven pro asynchronní programování

class HeaderAnalyzer  // Definice třídy pro analýzu HTTP hlaviček
{
    static async Task Main(string[] args)  // Hlavní asynchronní metoda programu
    {
        if (args.Length != 1)  // Kontrola počtu argumentů
        {
            // Výpis chybové zprávy při špatném počtu argumentů
            Console.WriteLine("Použití: HeaderAnalyzer <URL>");
            return;  // Předčasné ukončení programu
        }

        string url = args[0];  // Uložení URL z prvního argumentu

        using (HttpClient client = new HttpClient())  // Vytvoření HTTP klienta s automatickým uklizením
        {
            // Nastavení user-agent aby vypadal jako reálný prohlížeč
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            // Asynchronní odeslání GET požadavku na zadanou URL
            HttpResponseMessage response = await client.GetAsync(url);

            // Výpis HTTP stavového kódu
            Console.WriteLine($"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine("\nHlavičky:");  // Výpis nadpisu pro sekci hlaviček

            // Cyklus pro iterování přes všechny hlavičky odpovědi
            foreach (var header in response.Headers)
            {
                // Výpis názvu hlavičky a jejích hodnot (oddělených čárkou)
                Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }
        }
    }
}

/*
Knihovny a jmenné prostory:
 System - základní funkce .NET
 System.Net.Http - práce s HTTP protokolem
 System.Threading.Tasks - asynchronní operace
Práce s argumenty:
 Program kontroluje, zda byl předán přesně jeden argument (URL)
 Při chybném počtu vypíše návod k použití
HTTP komunikace:
 Používá HttpClient pro odesílání požadavků
 Přidává fake User-Agent hlavičku pro obejití základní ochrany před boty
 Asynchronně volá cílovou URL pomocí GetAsync()
Výpis výsledků:
 Zobrazí HTTP stavový kód (např. 200 OK)
 Vypíše všechny vrácené HTTP hlavičky ve formátu Název: Hodnota
*/