using System;           // Základní jmenný prostor pro konzoli, výjimky atd.
using System.Diagnostics; // Pro třídu Stopwatch (měření času)
using System.Net.Sockets; // Pro práci s TCP klientem a NetworkStream
using System.Text;      // Pro práci s textovými kodováními (v tomto kódu se přímo nepoužívá)
using System.Threading.Tasks; // Pro asynchronní operace (async/await)

class ThroughputTester  // Hlavní třída testující propustnost sítě
{
    static async Task Main(string[] args)  // Asynchronní vstupní bod aplikace
    {
        // Kontrola počtu argumentů - program vyžaduje 3 argumenty
        if (args.Length != 3)
        {
            Console.WriteLine("Použití: ThroughputTester <server> <port> <velikost_MB>");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        string server = args[0];  // První argument - IP adresa nebo název serveru
        int port = int.Parse(args[1]);  // Druhý argument - číslo portu (převod na číslo)
        int sizeMB = int.Parse(args[2]); // Třetí argument - velikost dat v MB (převod na číslo)

        // Vytvoření testovacích dat o velikosti zadané v MB
        // 1 MB = 1024 * 1024 bytů
        byte[] testData = new byte[sizeMB * 1024 * 1024];

        // Naplnění pole náhodnými daty pomocí generátoru pseudonáhodných čísel
        new Random().NextBytes(testData);

        // Vytvoření TCP klienta pomocí using pro automatické uvolnění prostředků
        using TcpClient client = new TcpClient();

        // Spuštění stopek pro měření doby odesílání
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Asynchronní připojení k cílovému serveru na zadaný port
        await client.ConnectAsync(server, port);

        // Získání síťového streamu pro odesílání dat
        NetworkStream stream = client.GetStream();

        // Asynchronní odeslání všech testovacích dat přes síťový stream
        await stream.WriteAsync(testData, 0, testData.Length);

        // Zastavení stopek po dokončení odesílání
        stopwatch.Stop();

        // Výpočet rychlosti přenosu v Mbps (megabitech za sekundu)
        // Délka dat v bytech * 8 = převedení na bity
        // / (stopwatch.Elapsed.TotalSeconds * 1000000) = převedení na Mbps
        double speed = (testData.Length * 8) / (stopwatch.Elapsed.TotalSeconds * 1000000);

        // Výpis výsledků měření
        Console.WriteLine($"Odesláno {sizeMB} MB za {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Průtok: {speed:F2} Mbps");
    }
}

/*
Kód měří pouze odesílací rychlost (upload)
Neověřuje, zda server data skutečně přijal - předpokládá úspěšný přenos
Pro přesnější měření by bylo vhodné implementovat i přijímací stranu
Rychlost se počítá v megabitech za sekundu (Mbps)
Data se generují náhodně, což může ovlivnit kompresi při přenosu
Kód využívá moderní C# prvky (async/await, using declaration)

Tento kód slouží jako základní nástroj pro testování síťové propustnosti.
*/