using System;  // Základní jmenný prostor pro Console, Exception a základní třídy
using System.Net.Sockets;  // TcpClient, NetworkStream, SocketException
using System.Text;  // Kódování textu (Encoding.UTF8) a StringBuilder
using System.Threading;  // CancellationTokenSource pro zrušení úloh, Thread.Sleep
using System.Threading.Tasks;  // Asynchronní programování (Task, async/await)

class AdvancedTelnetClient  // Telnet klient s podporou základního vyjednávání voleb (RFC 854)
{
    private TcpClient client;  // TCP spojení se serverem
    private NetworkStream stream;  // Síťový stream pro čtení a zápis dat
    private CancellationTokenSource cancellationTokenSource;  // Zdroj pro zrušení asynchronních operací
    private bool isConnected = false;  // Příznak stavu připojení

    // Telnet command constants (příkazy se posílají za bajtem IAC)
    private const byte IAC = 255;  // Interpret As Command - uvozuje telnetový příkaz
    private const byte DONT = 254;  // Žádost, aby druhá strana volbu nepoužívala
    private const byte DO = 253;  // Žádost, aby druhá strana volbu používala
    private const byte WONT = 252;  // Odmítnutí používat volbu
    private const byte WILL = 251;  // Nabídka, že volbu budeme používat
    private const byte SB = 250;   // Subnegotiation Begin
    private const byte SE = 240;   // Subnegotiation End
    private const byte ECHO = 1;  // Volba ECHO (v této ukázce se nevyjednává, echo řeší klient lokálně)
    private const byte SUPPRESS_GO_AHEAD = 3;  // Volba Suppress Go Ahead (plně duplexní komunikace)

    static async Task Main(string[] args)  // Asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje adresu a port
        if (args.Length < 2)
        {
            Console.WriteLine("Použití: AdvancedTelnetClient <hostname> <port>");
            Console.WriteLine("Příklad: AdvancedTelnetClient localhost 23");
            Console.WriteLine("Příklad: AdvancedTelnetClient telehack.com 23");
            return;  // Ukončení programu při nedostatku argumentů
        }

        string hostname = args[0];  // Adresa serveru
        int port = int.Parse(args[1]);  // Port serveru (převod na číslo)

        var telnetClient = new AdvancedTelnetClient();  // Instance klienta
        await telnetClient.ConnectAsync(hostname, port);  // Připojení a běh klienta do ukončení
    }

    // Připojení k serveru a spuštění úloh pro příjem a odesílání
    public async Task ConnectAsync(string hostname, int port)
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            client = new TcpClient();
            client.ReceiveTimeout = 5000;  // Časový limit pro synchronní čtení (ms)
            client.SendTimeout = 5000;  // Časový limit pro synchronní zápis (ms)

            Console.WriteLine($"Připojování k {hostname}:{port}...");

            await client.ConnectAsync(hostname, port);
            stream = client.GetStream();
            isConnected = true;

            // Ctrl+C se bude číst jako běžná klávesa, aby šlo spojení korektně ukončit (viz ReadLineWithCancel)
            Console.TreatControlCAsInput = true;

            Console.WriteLine($"Úspěšně připojeno k {hostname}:{port}");
            Console.WriteLine("Klávesové zkratky:");
            Console.WriteLine("  Ctrl+C nebo Esc - Ukončit spojení");
            Console.WriteLine("  QUIT - Ukončit spojení");
            Console.WriteLine("  CLEAR - Vyčistit obrazovku\n");

            // Spustíme úlohy pro příjem a odesílání, každou na vlastním vlákně
            var receiveTask = Task.Run(() => ReceiveDataAsync(cancellationTokenSource.Token));
            var sendTask = Task.Run(() => SendDataAsync(cancellationTokenSource.Token));

            await Task.WhenAny(receiveTask, sendTask);  // Čeká na dokončení kterékoli úlohy (odpojení nebo ukončení uživatelem)

            cancellationTokenSource.Cancel();  // Zrušení druhé úlohy
            await Task.WhenAll(receiveTask, sendTask);  // Čekání na korektní dokončení obou úloh
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Síťová chyba: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
        finally
        {
            Disconnect();  // Uvolnění prostředků proběhne vždy
        }
    }

    // Úloha pro příjem dat ze serveru
    private async Task ReceiveDataAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];  // Vyrovnávací paměť pro příchozí data

        try
        {
            while (isConnected && !cancellationToken.IsCancellationRequested)
            {
                if (stream.DataAvailable)  // Jsou k dispozici data ke čtení?
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        ProcessReceivedData(buffer, bytesRead);  // Odfiltrování telnetových příkazů a výpis textu
                    }
                    else
                    {
                        break;  // Server ukončil spojení
                    }
                }
                else
                {
                    // DataAvailable je false i po zavření spojení serverem - odpojení se pozná přes Poll
                    if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
                    {
                        Console.WriteLine("\nServer ukončil spojení.");
                        break;
                    }

                    await Task.Delay(10, cancellationToken);  // Krátká pauza, aby smyčka nevytěžovala procesor
                }
            }
        }
        catch (Exception ex)
        {
            if (isConnected)  // Chyba po vlastním ukončení spojení se nehlásí
            {
                Console.WriteLine($"\nChyba při příjmu: {ex.Message}");
            }
        }
    }

    // Stav parseru telnetových příkazů se drží mezi čteními (příkaz může být rozdělen mezi dva pakety)
    private int iacState = 0;  // 0 = běžná data, 1 = po IAC, 2 = čeká na option, 3 = subnegotiation, 4 = IAC uvnitř subnegotiation
    private byte iacCommand;  // Poslední přijatý příkaz (WILL/WONT/DO/DONT)
    private readonly Decoder utf8Decoder = Encoding.UTF8.GetDecoder();  // Zvládne UTF-8 znak rozdělený mezi dvě čtení

    // Projde přijatá data, telnetové příkazy zpracuje a běžný text vypíše
    private void ProcessReceivedData(byte[] data, int length)
    {
        var clean = new System.Collections.Generic.List<byte>(length);  // Data bez telnetových příkazů

        for (int i = 0; i < length; i++)
        {
            byte b = data[i];
            switch (iacState)
            {
                case 0:  // Běžná data
                    if (b == IAC) iacState = 1; else clean.Add(b);
                    break;
                case 1:  // Předchozí bajt byl IAC
                    if (b == IAC) { clean.Add(IAC); iacState = 0; }  // Escapovaný bajt 255
                    else if (b >= WILL && b <= DONT) { iacCommand = b; iacState = 2; }  // Čeká se na číslo volby
                    else if (b == SB) iacState = 3;  // Subnegotiation se přeskakuje až do IAC SE
                    else iacState = 0;  // Ostatní dvoubajtové příkazy se ignorují
                    break;
                case 2:  // Příkaz WILL/WONT/DO/DONT a číslo volby
                    ProcessTelnetCommand(iacCommand, b);
                    iacState = 0;
                    break;
                case 3:  // Uvnitř subnegotiation (data se zahazují)
                    if (b == IAC) iacState = 4;
                    break;
                case 4:  // V subnegotiation za IAC: SE ji ukončí
                    iacState = b == SE ? 0 : 3;
                    break;
            }
        }

        // Normální data - dekódovat jako UTF-8 (shodně s odesíláním) a vypsat na konzoli
        char[] chars = new char[clean.Count + 4];
        int count = utf8Decoder.GetChars(clean.ToArray(), 0, clean.Count, chars, 0);
        Console.Write(new string(chars, 0, count));
    }

    // Odpověď na vyjednávání voleb (option negotiation)
    private void ProcessTelnetCommand(byte command, byte option)
    {
        // Zde lze implementovat reakce na Telnet option negotiation
        // Podporujeme jen Suppress Go Ahead, ostatní volby odmítáme

        byte[] response = null;  // Odpověď, která se pošle serveru

        switch (command)
        {
            case DO:
                // Suppress Go Ahead podporujeme, ostatní volby (včetně ECHO - echo řešíme lokálně) odmítneme
                response = new byte[] { IAC, option == SUPPRESS_GO_AHEAD ? WILL : WONT, option };
                break;

            case WILL:
                // Server nabízí option - Suppress Go Ahead přijmeme, ostatní odmítneme
                response = new byte[] { IAC, option == SUPPRESS_GO_AHEAD ? DO : DONT, option };
                break;

            case WONT:
            case DONT:
                // Tyto příkazy můžeme ignorovat
                break;
        }

        if (response != null)
        {
            try
            {
                stream.Write(response, 0, response.Length);
            }
            catch
            {
                // Ignorovat chyby při odpovídání na option negotiation
            }
        }
    }

    // Úloha pro odesílání vstupu z klávesnice na server
    private async Task SendDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (isConnected && !cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)  // Stiskl uživatel klávesu?
                {
                    string input = ReadLineWithCancel();  // Načtení celého řádku

                    if (string.IsNullOrEmpty(input))  // Prázdný řádek se neodesílá
                        continue;

                    if (input.Trim().ToUpper() == "QUIT")  // Lokální příkaz pro ukončení
                    {
                        Console.WriteLine("Ukončování spojení...");
                        break;
                    }
                    else if (input.Trim().ToUpper() == "CLEAR")  // Lokální příkaz pro vymazání obrazovky
                    {
                        Console.Clear();
                        continue;
                    }

                    await SendStringAsync(input + "\r\n", cancellationToken);  // Telnet ukončuje řádky sekvencí CR LF
                }
                else
                {
                    await Task.Delay(10, cancellationToken);  // Krátká pauza, aby smyčka nevytěžovala procesor
                }
            }
        }
        catch (Exception ex)
        {
            if (isConnected)
            {
                Console.WriteLine($"\nChyba při odesílání: {ex.Message}");
            }
        }
    }

    // Odeslání textu na server
    private async Task SendStringAsync(string text, CancellationToken cancellationToken)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);  // Převod textu na bajty (UTF-8)
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);  // Okamžité odeslání dat
    }

    // Vlastní čtení řádku po jednotlivých klávesách (umožňuje přerušení při zrušení)
    private string ReadLineWithCancel()
    {
        StringBuilder input = new StringBuilder();  // Dosud zadaný text

        while (true)
        {
            // Čekání na klávesu s kontrolou zrušení (např. server zavřel spojení), ReadKey by jinak blokovalo
            while (!Console.KeyAvailable)
            {
                if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
                    return "QUIT";
                Thread.Sleep(20);
            }

            var keyInfo = Console.ReadKey(true);  // Načtení klávesy bez zobrazení

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:  // Konec řádku
                    Console.WriteLine();
                    return input.ToString();

                case ConsoleKey.Escape:  // Esc nebo Ctrl+C ukončí spojení
                case ConsoleKey.C when (keyInfo.Modifiers & ConsoleModifiers.Control) != 0:
                    Console.WriteLine("\nUkončování spojení...");
                    cancellationTokenSource?.Cancel();
                    return "QUIT";

                case ConsoleKey.Backspace when input.Length > 0:  // Smazání posledního znaku
                    input.Remove(input.Length - 1, 1);
                    Console.Write("\b \b");  // Posun kurzoru zpět, přepsání mezerou a opět zpět
                    break;

                case ConsoleKey.U when (keyInfo.Modifiers & ConsoleModifiers.Control) != 0:
                    // Ctrl+U - smazat celý řádek
                    while (input.Length > 0)
                    {
                        input.Remove(input.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))  // Pouze tisknutelné znaky
                    {
                        input.Append(keyInfo.KeyChar);
                        Console.Write(keyInfo.KeyChar);  // Lokální echo (server echo nevyjednáváme)
                    }
                    break;
            }
        }
    }

    // Uzavření spojení a uvolnění prostředků
    private void Disconnect()
    {
        isConnected = false;
        Console.TreatControlCAsInput = false;  // Obnovení běžného chování Ctrl+C

        try
        {
            stream?.Close();
            client?.Close();
        }
        catch
        {
            // Ignorovat chyby při zavírání
        }

        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();

        Console.WriteLine("Spojení ukončeno.");
    }
}

/*
Telnet (RFC 854):
 Textový protokol na TCP portu 23; řídicí příkazy se posílají v datech za bajtem IAC (255)
Vyjednávání voleb:
 Server posílá DO/DONT/WILL/WONT <volba>; klient odpovídá WILL/WONT/DO/DONT
 Tento klient podporuje jen Suppress Go Ahead, ostatní volby odmítá
Zpracování dat:
 Parser přijatých dat si pamatuje stav mezi čteními, takže příkaz rozdělený mezi dva pakety nevadí
 Subnegotiation (IAC SB ... IAC SE) se přeskakuje, IAC IAC je escapovaný bajt 255
 Text se dekóduje jako UTF-8 (stejně jako při odesílání)
Ovládání:
 Esc, Ctrl+C nebo příkaz QUIT ukončí spojení; CLEAR vymaže obrazovku
 Ukončení serverem klient pozná kontrolou socketu (Poll), protože DataAvailable to neumí
*/
