using System;  // Základní jmenný prostor pro základní funkce .NET (Console, Exception, atd.)
using System.Net.Sockets;  // Jmenný prostor pro práci se síťovými spojeními (TcpClient, NetworkStream)
using System.Text;  // Pro práci s textovými kodováními (Encoding.UTF8)
using System.Threading;  // Pro práci s vlákny (CancellationTokenSource, CancellationToken)
using System.Threading.Tasks;  // Pro práci s asynchronními operacemi (Task, async, await)

class TelnetClient  // Hlavní třída telnet klienta
{
    private TcpClient client;  // TcpClient pro připojení k serveru
    private NetworkStream stream;  // Síťový stream pro čtení a zápis dat
    private CancellationTokenSource cancellationTokenSource;  // Zdroj pro zrušení asynchronních operací
    private bool isConnected = false;  // Příznak indikující stav připojení

    static async Task Main(string[] args)  // Hlavní vstupní bod aplikace
    {
        if (args.Length < 2)  // Kontrola počtu argumentů příkazové řádky
        {
            Console.WriteLine("Použití: TelnetClient <hostname> <port>");  // Chybová zpráva při špatném počtu argumentů
            Console.WriteLine("Příklad: TelnetClient localhost 23");  // Ukázka použití
            return;  // Předčasné ukončení programu
        }

        string hostname = args[0];  // Získání hostname z prvního argumentu
        int port = int.Parse(args[1]);  // Získání portu z druhého argumentu a převod na číslo

        var telnetClient = new TelnetClient();  // Vytvoření instance telnet klienta
        await telnetClient.ConnectAsync(hostname, port);  // Asynchronní volání metody připojení
    }

    public async Task ConnectAsync(string hostname, int port)  // Metoda pro připojení k serveru
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();  // Inicializace zdroje tokenu pro zrušení operací
            client = new TcpClient();  // Vytvoření nové instance TcpClient

            Console.WriteLine($"Připojování k {hostname}:{port}...");  // Informace o pokusu o připojení

            await client.ConnectAsync(hostname, port);  // Asynchronní připojení k serveru
            stream = client.GetStream();  // Získání síťového streamu z připojeného klienta
            isConnected = true;  // Nastavení příznaku připojení na true

            // Ctrl+C se bude číst jako běžná klávesa, aby šlo spojení korektně ukončit (viz ReadLineWithCancel)
            Console.TreatControlCAsInput = true;

            Console.WriteLine($"Připojeno k {hostname}:{port}");  // Potvrzení úspěšného připojení
            Console.WriteLine("Pro ukončení napište 'QUIT' nebo stiskněte Ctrl+C\n");  // Nápověda pro ukončení

            // Spustíme úlohy pro čtení a zápis
            var receiveTask = Task.Run(() => ReceiveDataAsync(cancellationTokenSource.Token));  // Spuštění úlohy pro příjem dat na pozadí
            var sendTask = Task.Run(() => SendDataAsync(cancellationTokenSource.Token));  // Spuštění úlohy pro odesílání dat na pozadí

            // Čekáme na dokončení kterékoliv úlohy
            await Task.WhenAny(receiveTask, sendTask);  // Čekání na dokončení jedné z úloh (obvykle když se něco pokazí nebo uživatel ukončí spojení)

            cancellationTokenSource.Cancel();  // Požadavek na zrušení všech probíhajících operací
            await Task.WhenAll(receiveTask, sendTask);  // Čekání na korektní dokončení obou úloh
        }
        catch (Exception ex)  // Zachycení výjimek během připojování
        {
            Console.WriteLine($"Chyba při připojování: {ex.Message}");  // Výpis chyby
        }
        finally  // Blok, který se vždy vykoná (úspěch i neúspěch)
        {
            Disconnect();  // Ujistíme se, že spojení je ukončeno
        }
    }

    private async Task ReceiveDataAsync(CancellationToken cancellationToken)  // Metoda pro příjem dat ze serveru
    {
        byte[] buffer = new byte[4096];  // Vyrovnávací paměť pro příchozí data (4KB)

        try
        {
            while (isConnected && !cancellationToken.IsCancellationRequested)  // Smyčka dokud jsme připojeni a není požadováno zrušení
            {
                if (stream.DataAvailable)  // Kontrola zda jsou dostupná data ke čtení
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);  // Asynchronní čtení dat do bufferu
                    if (bytesRead > 0)  // Pokud bylo přečteno alespoň nějaké data
                    {
                        string data = ProcessTelnetData(buffer, bytesRead);  // Odstranění telnetových příkazů a převod z UTF-8
                        Console.Write(data);  // Výpis dat na konzoli
                    }
                    else
                    {
                        break;  // Server ukončil spojení
                    }
                }
                else  // Pokud nejsou dostupná data
                {
                    // DataAvailable je false i po zavření spojení serverem - odpojení se pozná přes Poll
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                    {
                        Console.WriteLine("\nServer ukončil spojení.");
                        break;
                    }

                    await Task.Delay(50, cancellationToken);  // Krátké čekání (50 ms) před kontrolou znovu (šetří prostředky)
                }
            }
        }
        catch (OperationCanceledException)  // Zachycení výjimky při zrušení operace
        {
            // Očekávané při zrušení - není potřeba nic dělat
        }
        catch (Exception ex)  // Zachycení ostatních výjimek
        {
            if (isConnected)  // Pokud jsme stále připojeni (výjimka nebyla způsobena ukončením spojení)
            {
                Console.WriteLine($"\nChyba při příjmu dat: {ex.Message}");  // Výpis chyby příjmu
            }
        }
    }

    // Stav zpracování telnetových příkazů (zachovává se mezi voláními, protože příkaz může být rozdělen mezi dva čtení)
    private int iacState = 0;  // 0 = běžná data, 1 = po IAC, 2 = čeká na option, 3 = subnegotiation, 4 = IAC uvnitř subnegotiation
    private byte iacCommand;  // Poslední příkaz (WILL/WONT/DO/DONT)
    private readonly Decoder utf8Decoder = Encoding.UTF8.GetDecoder();  // Dekodér zvládne UTF-8 znak rozdělený mezi dvě čtení

    // Odstraní telnetové řídicí sekvence (IAC ...), na žádosti o volby odpoví odmítnutím a vrátí čistý text
    private string ProcessTelnetData(byte[] data, int length)
    {
        const byte IAC = 255, SE = 240, SB = 250;
        var clean = new System.Collections.Generic.List<byte>(length);

        for (int i = 0; i < length; i++)
        {
            byte b = data[i];
            switch (iacState)
            {
                case 0:
                    if (b == IAC) iacState = 1; else clean.Add(b);
                    break;
                case 1:
                    if (b == IAC) { clean.Add(IAC); iacState = 0; }  // Escapovaný bajt 255
                    else if (b >= 251 && b <= 254) { iacCommand = b; iacState = 2; }
                    else if (b == SB) iacState = 3;
                    else iacState = 0;
                    break;
                case 2:
                    // Odmítnutí všech voleb: DO -> WONT, WILL -> DONT
                    if (iacCommand == 253 || iacCommand == 251)
                    {
                        byte[] reply = { IAC, iacCommand == 253 ? (byte)252 : (byte)254, b };
                        try { stream.Write(reply, 0, reply.Length); } catch { }
                    }
                    iacState = 0;
                    break;
                case 3:
                    if (b == IAC) iacState = 4;
                    break;
                case 4:
                    iacState = b == SE ? 0 : 3;
                    break;
            }
        }

        char[] chars = new char[clean.Count + 4];
        int count = utf8Decoder.GetChars(clean.ToArray(), 0, clean.Count, chars, 0);
        return new string(chars, 0, count);
    }

    private async Task SendDataAsync(CancellationToken cancellationToken)  // Metoda pro odesílání dat na server
    {
        try
        {
            while (isConnected && !cancellationToken.IsCancellationRequested)  // Smyčka dokud jsme připojeni a není požadováno zrušení
            {
                if (Console.KeyAvailable)  // Kontrola zda uživatel zadal vstup z klávesnice
                {
                    string input = ReadLineWithCancel();  // Načtení vstupu s podporou zrušení

                    if (string.IsNullOrEmpty(input))  // Pokud byl vstup prázdný
                        continue;  // Pokračujeme dalším cyklem

                    if (input.Trim().ToUpper() == "QUIT")  // Kontrola zda uživatel zadal příkaz QUIT
                    {
                        Console.WriteLine("Ukončování spojení...");  // Informace o ukončování
                        break;  // Přerušení smyčky odesílání
                    }

                    byte[] data = Encoding.UTF8.GetBytes(input + "\r\n");  // Převod textu na bajty + přidání CRLF (telnet formát)
                    await stream.WriteAsync(data, 0, data.Length, cancellationToken);  // Asynchronní zápis dat na síťový stream
                    await stream.FlushAsync(cancellationToken);  // Okamžité odeslání dat (nečekání na naplnění bufferu)
                }
                else  // Pokud není dostupný vstup z klávesnice
                {
                    await Task.Delay(50, cancellationToken);  // Krátké čekání (50 ms) před kontrolou znovu
                }
            }
        }
        catch (OperationCanceledException)  // Zachycení výjimky při zrušení operace
        {
            // Očekávané při zrušení - není potřeba nic dělat
        }
        catch (Exception ex)  // Zachycení ostatních výjimek
        {
            if (isConnected)  // Pokud jsme stále připojeni
            {
                Console.WriteLine($"\nChyba při odesílání dat: {ex.Message}");  // Výpis chyby odesílání
            }
        }
    }

    private string ReadLineWithCancel()  // Vlastní implementace čtení řádku s podporou zrušení
    {
        StringBuilder input = new StringBuilder();  // StringBuilder pro efektivní skládání řetězce
        ConsoleKeyInfo keyInfo;  // Informace o stisknuté klávese

        while (true)  // Nekonečná smyčka pro čtení kláves
        {
            // Čekání na klávesu s kontrolou zrušení (např. server zavřel spojení), ReadKey by jinak blokovalo
            while (!Console.KeyAvailable)
            {
                if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
                    return "QUIT";
                Thread.Sleep(20);
            }

            keyInfo = Console.ReadKey(true);  // Načtení klávesy bez zobrazení (intercept = true)

            if (keyInfo.Key == ConsoleKey.Enter)  // Pokud byla stisknuta Enter
            {
                Console.WriteLine();  // Odřádkování
                return input.ToString();  // Vrácení načteného řetězce
            }
            else if (keyInfo.Key == ConsoleKey.Escape ||  // Pokud byla stisknuta Escape
                    (keyInfo.Key == ConsoleKey.C && (keyInfo.Modifiers & ConsoleModifiers.Control) != 0))  // Nebo Ctrl+C
            {
                Console.WriteLine("\nUkončování...");  // Informace o ukončování
                cancellationTokenSource?.Cancel();  // Aktivace zrušení operací (pokud existuje zdroj)
                return "QUIT";  // Vrácení příkazu QUIT pro ukončení
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)  // Pokud byl stisknut Backspace a máme co mazat
            {
                input.Remove(input.Length - 1, 1);  // Odstranění posledního znaku z bufferu
                Console.Write("\b \b");  // Mazání znaku na konzoli (cursor back, space, cursor back)
            }
            else if (!char.IsControl(keyInfo.KeyChar))  // Pokud je znak tisknutelný (ne řídící znak)
            {
                input.Append(keyInfo.KeyChar);  // Přidání znaku do bufferu
                Console.Write(keyInfo.KeyChar);  // Zobrazení znaku na konzoli
            }
        }
    }

    private void Disconnect()  // Metoda pro bezpečné ukončení spojení
    {
        isConnected = false;  // Nastavení příznaku připojení na false
        Console.TreatControlCAsInput = false;  // Obnovení běžného chování Ctrl+C

        try  // Pokus o bezpečné uzavření prostředků
        {
            stream?.Close();  // Uzavření síťového streamu (pokud existuje)
            client?.Close();  // Uzavření TcpClient (pokud existuje)
            cancellationTokenSource?.Cancel();  // Aktivace zrušení (pro jistotu)
        }
        catch  // Zachycení případných chyb při uzavírání
        {
            // Ignorovat chyby při zavírání - důležité pro bezpečné ukončení
        }

        Console.WriteLine("\nSpojení ukončeno.");  // Informace o ukončení spojení
    }
}

/*
Inicializace připojení - Na základě argumentů příkazové řádky
Asynchronní operace - Simultánní čtení a zápis pomocí Tasks
Zpracování příjmu dat - Průběžné čtení ze síťového streamu; telnetové příkazy (IAC) se odfiltrují a všechny volby se odmítnou (WILL -> DONT, DO -> WONT)
Detekce odpojení - Zavření spojení serverem se pozná kontrolou socketu (Poll), DataAvailable to neumí
Zpracování odesílání dat - Čtení vstupu z konzole a odesílání na server
Podpora ukončení - Příkaz QUIT nebo Ctrl+C/Escape
Ošetření chyb - Robustní zachycování výjimek
Bezpečné ukončování - Korektní uvolnění všech prostředků

Kód implementuje plně funkční Telnet klient s uživatelským rozhraním v konzoli.
*/