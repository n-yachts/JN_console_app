using System;           // Základní jmenný prostor pro vstupy/výstupy, řetězce, výjimky atd.
using System.IO;        // Práce se soubory a adresáři (File, Directory, Path)
using System.Net;       // Síťové funkce (IPAddress, Dns)
using System.Net.Sockets; // TCP/IP komunikace (TcpListener, TcpClient)
using System.Text;      // Kódování textu (Encoding.UTF8)
using System.Threading.Tasks; // Asynchronní operace (Task, async/await)

class SimpleFileServer  // Hlavní třída souborového serveru
{
    static async Task Main(string[] args) // Hlavní asynchronní vstupní bod programu
    {
        int port = 8080;  // Nastavení portu pro naslouchání serveru
        string shareDirectory = "./shared"; // Cesta k sdílenému adresáři

        Directory.CreateDirectory(shareDirectory); // Vytvoření adresáře, pokud neexistuje

        TcpListener listener = new TcpListener(IPAddress.Any, port); // Inicializace TCP listeneru pro všechny síťové rozhraní
        listener.Start(); // Spuštění naslouchání příchozích spojení

        Console.WriteLine($"File server běží na portu {port}"); // Informace o spuštění serveru
        Console.WriteLine($"Sdílený adresář: {Path.GetFullPath(shareDirectory)}"); // Absolutní cesta k adresáři

        while (true)  // Nekonečná smyčka pro přijímání klientů
        {
            TcpClient client = await listener.AcceptTcpClientAsync(); // Asynchronní přijetí nového klienta
            _ = Task.Run(() => HandleClient(client, shareDirectory)); // Spuštění obsluhy klienta na vlákně z ThreadPoolu
        }
    }

    static async Task HandleClient(TcpClient client, string shareDir) // Metoda pro obsluhu klienta
    {
        using (client) // Automatické uvolnění prostředků klienta po dokončení
        using (NetworkStream stream = client.GetStream()) // Získání síťového streamu pro komunikaci
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) // Čtečka pro příjem textu
        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8)) // Zapisovač pro odesílání textu
        {
            try
            {
                // Získání seznamu souborů v adresáři
                string[] files = Directory.GetFiles(shareDir); // Načtení všech souborů v adresáři
                await writer.WriteLineAsync("Dostupné soubory:"); // Hlavička seznamu
                foreach (string file in files) // Procházení všech souborů
                {
                    await writer.WriteLineAsync($"  {Path.GetFileName(file)}"); // Zápis jména souboru (bez cesty)
                }
                await writer.WriteLineAsync("END"); // Ukončovací značka seznamu
                await writer.FlushAsync(); // Okamžité odeslání dat z bufferu
            }
            catch (Exception ex) // Zachycení všech výjimek
            {
                await writer.WriteLineAsync($"Chyba: {ex.Message}"); // Odeslání chybové zprávy klientovi
                await writer.FlushAsync(); // Vynucení odeslání
            }
        } // Všechny disposovatelné prostředky jsou automaticky uvolněny
    }
}

/*
Inicializace serveru:
 Port 8080 a adresář ./shared jsou hardcoded
 Adresář se vytváří při spuštění, pokud neexistuje
 IPAddress.Any znamená naslouchání na všech síťových rozhraních
Přijímání klientů:
 AcceptTcpClientAsync() blokuje čekáním na nové připojení
 Pro každého klienta se spustí asynchronní úloha (Task.Run)
 Použití _ = ignoruje vrácený Task (v production kódu by se měly ošetřovat výjimky)
Komunikace s klientem:
 Všechny síťové prostředky jsou v using blocích pro správné uvolnění
 Server vždy pošle seznam souborů a poté ukončí spojení
 Formát odpovědi: hlavička, položky souborů, ukazatel konce "END"
Omezení:
 Neimplementuje HTTP protokol
 Neumožňuje skutečné stahování souborů
 Jednoduché textové protokoly bez zabezpečení
*/