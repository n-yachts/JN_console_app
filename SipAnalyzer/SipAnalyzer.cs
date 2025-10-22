using System;  // Import základních systémových funkcí a tříd
using System.Net;  // Import síťových funkcí (IP adresa, síťové protokoly)
using System.Net.Sockets;  // Import socketů pro síťovou komunikaci
using System.Text;  // Import práce s textovými encodingy (UTF-8)
using System.Threading.Tasks;  // Import asynchronního programování

class SipAnalyzer  // Hlavní třída pro analýzu SIP komunikace
{
    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje přesně 1 argument
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: SipAnalyzer <local_port>");
            Console.WriteLine("Příklad: SipAnalyzer 5060");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        int port = int.Parse(args[0]);  // Převod textového argumentu na číslo portu

        // Informace o spuštění aplikace
        Console.WriteLine($"SIP Analyzer naslouchá na portu {port}...\n");
        Console.WriteLine("Stiskněte Ctrl+C pro ukončení.\n");

        await StartSipListener(port);  // Spuštění hlavní smyčky naslouchání
    }

    static async Task StartSipListener(int port)  // Metoda pro naslouchání SIP zprávám
    {
        // Vytvoření UDP socketu pro daný port s automatickým uvolněním prostředků (using)
        using (UdpClient listener = new UdpClient(port))
        {
            // Nekonečná smyčka pro průběžné přijímání zpráv
            while (true)
            {
                try
                {
                    // Asynchronní čekání na příchod datagramu
                    UdpReceiveResult result = await listener.ReceiveAsync();
                    // Převod přijatých bajtů na textový řetězec v UTF-8
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    // Hlavička pro vizuální oddělení zpráv
                    Console.WriteLine($"=== SIP Zpráva od {result.RemoteEndPoint} ===");
                    ParseSipMessage(message);  // Zpracování a analýza SIP zprávy
                    Console.WriteLine("=== Konec zprávy ===\n");
                }
                catch (Exception ex)  // Zachycení všech možných chyb
                {
                    Console.WriteLine($"Chyba při přijímání: {ex.Message}");
                }
            }
        }
    }

    static void ParseSipMessage(string message)  // Metoda pro analýzu SIP zprávy
    {
        // Rozdělení zprávy na jednotlivé řádky pomocí znaku nového řádku
        string[] lines = message.Split('\n');

        // Zpracování prvního řádku (request/response line)
        if (lines.Length > 0)
        {
            // Odebrání přebytečných bílých znaků z prvního řádku
            string firstLine = lines[0].Trim();
            Console.WriteLine($"První řádek: {firstLine}");

            // Rozlišení typu zprávy (Response vs Request)
            if (firstLine.StartsWith("SIP/2.0"))
            {
                Console.WriteLine("📨 SIP Response");
                string[] parts = firstLine.Split(' ');  // Rozdělení řádku podle mezer
                if (parts.Length >= 2)
                    Console.WriteLine($"Status: {parts[1]}");  // Výpis stavového kódu
            }
            else
            {
                Console.WriteLine("📤 SIP Request");
                string[] parts = firstLine.Split(' ');
                if (parts.Length >= 1)
                    Console.WriteLine($"Metoda: {parts[0]}");  // Výpis SIP metody (INVITE, REGISTER atd.)
            }
        }

        // Sekce pro zpracování hlaviček
        Console.WriteLine("\nHlavičky:");
        // Procházení řádků od druhého až do prvního prázdného řádku
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) break;  // Konec hlaviček při prázdném řádku

            // Výpis pouze důležitých hlaviček
            if (line.StartsWith("Via:") || line.StartsWith("From:") || line.StartsWith("To:") ||
                line.StartsWith("Call-ID:") || line.StartsWith("CSeq:") || line.StartsWith("Contact:"))
            {
                Console.WriteLine($"  {line}");
            }
        }

        // Sekce pro zpracování těla zprávy (SDP - Session Description Protocol)
        bool inBody = false;  // Příznak že jsme dosáhli těla zprávy
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line.Trim()))
            {
                inBody = true;  // Prázdný řádek odděluje hlavičky od těla
                continue;
            }

            if (inBody)  // Zpracování řádků za prázdným oddělovačem
            {
                // Výpis pouze důležitých SDP polí
                if (line.StartsWith("v=") || line.StartsWith("o=") || line.StartsWith("s=") ||
                    line.StartsWith("c=") || line.StartsWith("m=") || line.StartsWith("a="))
                {
                    Console.WriteLine($"SDP: {line}");
                }
            }
        }
    }
}

/*
Struktura programu:
 Aplikace je navržena jako jednoduchý SIP sniffer
 Používá UDP socket pro zachytávání SIP zpráv
 Pracuje s textovou formou SIP protokolu
Klíčové komponenty:
 Main() - inicializace a kontrola parametrů
 StartSipListener() - síťová vrstva pro příjem zpráv
 ParseSipMessage() - aplikační vrstva pro analýzu SIP
Zpracování zpráv:
 Rozlišuje SIP Requesty a Responses
 Extrahuje důležité hlavičky
 Parsuje SDP část pro informace o multimédiích
Síťová komunikace:
 Používá connectionless UDP protokol
 Asynchronní operace pro neblokující příjem
 Automatické uvolnění síťových prostředků
Výstupy:
 Formátovaný výpis s vizuálními oddělovači
 Rozlišení základních SIP komponent
 Informace o zdrojové adrese a portu
*/