using System;                   // Základní jmenný prostor pro vstup/výstup, výjimky atd.
using System.Collections.Generic; // Pro práci s kolekcemi (List, HashSet)
using System.Net.Http;          // Pro HTTP volání (HttpClient)
using System.Text.RegularExpressions; // Pro práci s regulárními výrazy
using System.Threading.Tasks;   // Pro asynchronní programování

class SimpleWebCrawler
{
    // Sada (HashSet) pro ukládání již navštívených URL - zabraňuje duplicitním návštěvám
    static HashSet<string> visitedUrls = new HashSet<string>();

    // HTTP klient pro stahování webového obsahu (používá se asynchronně)
    static HttpClient client = new HttpClient();

    // Hlavní vstupní bod programu (asynchronní)
    static async Task Main(string[] args)
    {
        // Kontrola, zda byl zadán přesně jeden argument (startovní URL)
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: SimpleWebCrawler <start_url>");
            return; // Ukončení programu při chybném počtu argumentů
        }

        string startUrl = args[0];  // Získání startovní URL z prvního argumentu

        if (!Uri.TryCreate(startUrl, UriKind.Absolute, out Uri startUri) ||
            (startUri.Scheme != Uri.UriSchemeHttp && startUri.Scheme != Uri.UriSchemeHttps))
        {
            Console.WriteLine("Neplatná URL. Zadejte absolutní adresu, např. https://example.com");
            return;
        }

        startHost = startUri.Host;  // Procházení se omezí na doménu startovní stránky
        await Crawl(startUrl, 0);   // Spuštění procházení (asynchronně)
    }

    // Omezení procházení: pouze startovní doména, maximální hloubka a počet stránek
    static string startHost;
    const int MaxDepth = 3;
    const int MaxPages = 50;

    // Hlavní rekurzivní metoda pro procházení webu
    static async Task Crawl(string url, int depth)
    {
        // Kontrola limitů a cizí domény
        if (depth > MaxDepth || visitedUrls.Count >= MaxPages)
            return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) ||
            !uri.Host.Equals(startHost, StringComparison.OrdinalIgnoreCase))
            return;

        // Normalizace (bez #fragmentu), aby se tatáž stránka nenavštívila vícekrát
        url = uri.GetLeftPart(UriPartial.Query);

        // Kontrola, zda jsme již URL navštívili - pokud ano, ukončí metodu
        if (visitedUrls.Contains(url))
            return;

        visitedUrls.Add(url);      // Přidání URL do seznamu navštívených
        Console.WriteLine($"Navštíveno: {url}"); // Výpis aktuálně zpracovávané URL

        try
        {
            // Asynchronní stažení HTML obsahu stránky
            string html = await client.GetStringAsync(url);

            // Extrakce všech odkazů z HTML pomocí pomocné metody
            var links = ExtractLinks(html, url);

            // Rekurzivní procházení všech nalezených odkazů
            foreach (string link in links)
            {
                await Crawl(link, depth + 1); // Asynchronní volání sebe sama pro každý odkaz
            }
        }
        catch (Exception ex) // Zachycení výjimek (např. chyba sítě, neplatná URL)
        {
            Console.WriteLine($"Chyba při načítání {url}: {ex.Message}");
        }
    }

    // Metoda pro extrakci odkazů z HTML kódu
    static List<string> ExtractLinks(string html, string baseUrl)
    {
        var links = new List<string>(); // Seznam pro ukládání nalezených odkazů

        // Regulární výraz pro hledání HTML tagů <a> s atributem href
        // Vysvětlení vzoru:
        // <a\s+               - začátek tagu <a s mezerami
        // (?:[^>]*?\s+)?      - nepřichycující skupina pro případné další atributy
        // href="([^"]*)"      - hledání href atributu s hodnotou v uvozovkách
        var regex = new Regex(@"<a\s+(?:[^>]*?\s+)?href=""([^""]*)""", RegexOptions.IgnoreCase);

        // Provedení shody regulárního výrazu s HTML obsahem
        MatchCollection matches = regex.Matches(html);

        // Zpracování všech nalezených shod
        foreach (Match match in matches)
        {
            // Získání hodnoty zachycené skupiny (obsah href atributu)
            string link = match.Groups[1].Value;

            // Zpracování absolutní URL (začíná na http/https)
            if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                link.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                links.Add(link); // Přidání přímo do seznamu
            }
            // Zpracování relativních URL ("/cesta", "stranka.html", "../x"); kotvy a schémata (mailto:, javascript:) se přeskočí
            else if (link.Length > 0 && !link.StartsWith("#") && !link.Contains(":"))
            {
                // Vytvoření absolutní URL kombinací základní URL a relativní cesty
                Uri baseUri = new Uri(baseUrl);
                links.Add(new Uri(baseUri, link).AbsoluteUri);
            }
        }

        return links; // Návrat seznamu absolutních URL
    }
}

/*
Crawler je omezen na startovní doménu (max. hloubka 3, max. 50 stránek), ale nemá omezení rychlosti
Může způsobit vysokou zátěž serverům
Nerespektuje robots.txt
Navštívené URL se evidují (bez #fragmentu), takže se nezacyklí
Pro produkční použití je nutné přidat:
Omezení počtu požadavků za sekundu
Respektování robots.txt
Ukládání stavu pro případné přerušení

robots.txt je soubor v kořenovém adresáři webového serveru, který obsahuje pokyny pro webové crawlerry (vyhledávací roboty) o tom,
           které části webu mohou nebo nemohou procházet.

Účel a význam
 Etická direktiva - Říká robotům, které stránky by neměly indexovat
 Ochrana zátěže - Chrání server před přílišnou zátěží z crawlerů
 Právní ochrana - Může sloužit jako důkaz o záměru omezit přístup
*/