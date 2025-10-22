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

                // Odeslání požadavku a získání odpovědi
                SearchResponse response = (SearchResponse)connection.SendRequest(request);

                Console.WriteLine($"Nalezeno {response.Entries.Count} objektů:\n");

                // Cyklus přes všechny nalezené záznamy
                foreach (SearchResultEntry entry in response.Entries)
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
                            Console.Write($"{value} ");  // Výpis hodnoty atributu
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
 Připojení není šifrované (pouze Basic autentizace)
 V produkčním prostředí doporučeno použít SSL/TLS
 Citlivé údaje (heslo) se předávají jako argument
Využití:
 Nástroj pro diagnostiku LDAP
 Prohlížení struktury adresáře
 Testování přihlašovacích údajů
Možná vylepšení:
 Přidání podpory SSL
 Implementace stránkování pro velké výsledky
 Filtrování atributů
 Podpora více autentizačních metod
*/