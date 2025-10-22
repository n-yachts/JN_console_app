using System;           // Základní jmenný prostor pro vstupy/výstupy, výjimky atd.
using System.IO;        // Práce se soubory a datovými toky
using System.Net;       // Síťové funkce včetně FTP
using System.Threading.Tasks;  // Asynchronní operace

class SimpleFTPClient   // Hlavní třída FTP klienta
{
    static async Task Main(string[] args)  // Asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje server, uživatelské jméno a heslo
        if (args.Length < 3)
        {
            Console.WriteLine("Použití: SimpleFTPClient <server> <username> <password>");
            return;  // Ukončení programu při nedostatku argumentů
        }

        // Načtení parametrů z příkazové řádky
        string server = args[0];    // první argument - adresa serveru
        string username = args[1];  // druhý argument - přihlašovací jméno
        string password = args[2];  // třetí argument - heslo

        try  // Ošetření možných chyb při připojování
        {
            // Vytvoření FTP požadavku na kořenový adresář serveru
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{server}/");

            // Nastavení přihlašovacích údajů
            request.Credentials = new NetworkCredential(username, password);

            // Volba metody pro výpis obsahu adresáře
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            // Asynchronní odeslání požadavku a získání odpovědi
            using FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();

            // Získání datového toku z odpovědi
            using Stream responseStream = response.GetResponseStream();

            // Čtečka pro práci s textovými daty z toku
            using StreamReader reader = new StreamReader(responseStream);

            // Výpis stavové zprávy z odpovědi serveru
            Console.WriteLine($"Stav: {response.StatusDescription}");
            Console.WriteLine("Obsah adresáře:");

            // Čtení prvního řádku ze seznamu
            string line = reader.ReadLine();

            // Cyklus pro čtení všech řádků v odpovědi
            while (line != null)
            {
                Console.WriteLine(line);  // Výpis aktuálního řádku
                line = reader.ReadLine(); // Načtení dalšího řádku
            }
        }
        catch (Exception ex)  // Zachycení všech možných chyb
        {
            // Výpis chybové zprávy (např. chybné přihlašovací údaje, nedostupný server)
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}

/*
Kontrola argumentů: Program vyžaduje tři povinné parametry - adresu serveru, uživatelské jméno a heslo.
Připojení k serveru:
 Vytvoří se FTP požadavek pomocí FtpWebRequest
 Přihlašovací údaje se nastaví pomocí NetworkCredential
 Metoda ListDirectory specifikuje, že chceme získat seznam souborů
Zpracování odpovědi:
 Připojení je asynchronní (await request.GetResponseAsync())
 Datový proud se čte pomocí StreamReader
 Program čte řádek po řádku až do konce souboru
Ošetření chyb:
 try-catch blok zachytává chyby sítě, autentizace a další výjimky
*/