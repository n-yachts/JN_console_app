using System;  // Import základních systémových knihoven
using System.DirectoryServices.Protocols;  // Import knihovny pro práci s LDAP protokolem
using System.Net;  // Import knihoven pro síťové funkce (včetně NetworkCredential)

class LdapBrowser  // Hlavní třída aplikace
{
    static void Main(string[] args)  // Hlavní vstupní bod aplikace
    {
        // Kontrola počtu argumentů - pokud je méně než 4, zobrazí nápovědu
        if (args.Length < 4)
        {
            Console.WriteLine("Použití: LdapBrowser <server> <port> <username> <password> [searchBase]");
            Console.WriteLine("Příklad: LdapBrowser ldap.company.com 389 cn=admin,dc=company,dc=com password dc=company,dc=com");
            return;  // Ukončení programu při nedostatku argumentů
        }

        // Načtení parametrů z příkazové řádky
        string server = args[0];        // První argument: adresa serveru
        int port = int.Parse(args[1]);  // Druhý argument: port (převod na číslo)
        string username = args[2];      // Třetí argument: uživatelské jméno
        string password = args[3];      // Čtvrtý argument: heslo
        // Pátý argument (volitelný): základ vyhledávání, pokud není zadán, použije se prázdný řetězec
        string searchBase = args.Length > 4 ? args[4] : "";

        try  // Ošetření možných chyb při připojování a práci s LDAP
        {
            // Vytvoření identifikátoru LDAP serveru s adresou a portem
            LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier(server, port);

            // Vytvoření spojení s LDAP serverem (using zajišťuje automatické uvolnění zdrojů)
            using (LdapConnection connection = new LdapConnection(identifier))
            {
                // Nastavení přihlašovacích údajů
                connection.Credential = new NetworkCredential(username, password);
                connection.AuthType = AuthType.Basic;  // Základní autentizace
                connection.SessionOptions.ProtocolVersion = 3;  // Verze LDAP protokolu 3

                // Basic autentizace posílá heslo v čitelné podobě, proto se na portu 636 zapne SSL
                // a na ostatních portech se uživatel upozorní
                if (port == 636)
                    connection.SessionOptions.SecureSocketLayer = true;
                else
                    Console.WriteLine("⚠️  Spojení není šifrované - heslo se posílá v čitelné podobě (použijte port 636).");

                Console.WriteLine($"Připojování k LDAP serveru {server}:{port}...");
                connection.Bind();  // Provedení skutečného připojení k serveru
                Console.WriteLine("✅ Připojení úspěšné\n");

                // Vytvoření požadavku na vyhledávání v LDAP
                SearchRequest request = new SearchRequest(
                    searchBase,          // Základní uzel pro vyhledávání
                    "(objectClass=*)",  // Filtr - všechny objekty
                    SearchScope.Subtree,// Rekurzivní vyhledávání v celém podstromu
                    null                // Vracet všechny atributy
                );

                // Stránkování výsledků (server jinak obvykle omezí počet vrácených záznamů, např. na 1000)
                var pageControl = new PageResultRequestControl(500);
                request.Controls.Add(pageControl);

                // Sběr záznamů ze všech stránek
                var entries = new System.Collections.Generic.List<SearchResultEntry>();
                while (true)
                {
                    // Odeslání požadavku a získání odpovědi
                    SearchResponse response = (SearchResponse)connection.SendRequest(request);
                    foreach (SearchResultEntry e in response.Entries)
                        entries.Add(e);

                    // Server vrací cookie další stránky; prázdná cookie = konec
                    var pageResponse = (PageResultResponseControl)Array.Find(
                        response.Controls, c => c is PageResultResponseControl);
                    if (pageResponse == null || pageResponse.Cookie.Length == 0)
                        break;
                    pageControl.Cookie = pageResponse.Cookie;
                }

                Console.WriteLine($"Nalezeno {entries.Count} objektů:\n");

                // Cyklus přes všechny nalezené záznamy
                foreach (SearchResultEntry entry in entries)
                {
                    Console.WriteLine($"DN: {entry.DistinguishedName}");  // Výpis DN (Distinguished Name)
                    Console.WriteLine("Atributy:");

                    // Cyklus přes všechny atributy záznamu
                    foreach (string attributeName in entry.Attributes.AttributeNames)
                    {
                        DirectoryAttribute attribute = entry.Attributes[attributeName];
                        Console.Write($"  {attributeName}: ");  // Název atributu

                        // Cyklus přes všechny hodnoty atributu (atribut může mít více hodnot)
                        foreach (object value in attribute)
                        {
                            // Binární hodnoty (byte[]) se vypíší jako hex místo "System.Byte[]"
                            string text = value is byte[] bytes ? BitConverter.ToString(bytes) : value.ToString();
                            Console.Write($"{text} ");  // Výpis hodnoty atributu
                        }
                        Console.WriteLine();  // Nový řádek za všemi hodnotami atributu
                    }
                    Console.WriteLine();  // Prázdný řádek mezi jednotlivými záznamy
                }
            }
        }
        catch (LdapException ex)  // Specifická výjimka pro LDAP chyby
        {
            Console.WriteLine($"LDAP Chyba: {ex.Message} (Error code: {ex.ErrorCode})");
        }
        catch (Exception ex)  // Obecná výjimka pro ostatní chyby
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}

/*
Struktura aplikace:
 Konzolová aplikace pro procházení LDAP adresáře
 Používá moderní DirectoryServices.Protocols namísto staršího DirectoryServices
Bezpečnostní aspekty:
 Basic autentizace posílá heslo v čitelné podobě; na portu 636 se proto zapíná SSL, na jiných portech program varuje
 V produkčním prostředí doporučeno použít SSL/TLS
 Citlivé údaje (heslo) se předávají jako argument
Využití:
 Nástroj pro diagnostiku LDAP
 Prohlížení struktury adresáře
 Testování přihlašovacích údajů
Možná vylepšení:
 Ověření serverového certifikátu a podpora StartTLS
 (stránkování výsledků po 500 záznamech je již implementováno)
 Filtrování atributů
 Podpora více autentizačních metod
*/