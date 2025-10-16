using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class AdvancedTelnetClient
{
    private TcpClient client;
    private NetworkStream stream;
    private CancellationTokenSource cancellationTokenSource;
    private bool isConnected = false;

    // Telnet command constants
    private const byte IAC = 255;  // Interpret As Command
    private const byte DONT = 254;
    private const byte DO = 253;
    private const byte WONT = 252;
    private const byte WILL = 251;
    private const byte SB = 250;   // Subnegotiation Begin
    private const byte SE = 240;   // Subnegotiation End
    private const byte ECHO = 1;
    private const byte SUPPRESS_GO_AHEAD = 3;

    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Použití: AdvancedTelnetClient <hostname> <port>");
            Console.WriteLine("Příklad: AdvancedTelnetClient localhost 23");
            Console.WriteLine("Příklad: AdvancedTelnetClient telehack.com 23");
            return;
        }

        string hostname = args[0];
        int port = int.Parse(args[1]);

        var telnetClient = new AdvancedTelnetClient();
        await telnetClient.ConnectAsync(hostname, port);
    }

    public async Task ConnectAsync(string hostname, int port)
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            client = new TcpClient();
            client.ReceiveTimeout = 5000;
            client.SendTimeout = 5000;

            Console.WriteLine($"Připojování k {hostname}:{port}...");

            await client.ConnectAsync(hostname, port);
            stream = client.GetStream();
            isConnected = true;

            Console.WriteLine($"Úspěšně připojeno k {hostname}:{port}");
            Console.WriteLine("Klávesové zkratky:");
            Console.WriteLine("  Ctrl+C nebo Esc - Ukončit spojení");
            Console.WriteLine("  QUIT - Ukončit spojení");
            Console.WriteLine("  CLEAR - Vyčistit obrazovku\n");

            // Spustíme úlohy
            var receiveTask = Task.Run(() => ReceiveDataAsync(cancellationTokenSource.Token));
            var sendTask = Task.Run(() => SendDataAsync(cancellationTokenSource.Token));

            await Task.WhenAny(receiveTask, sendTask);

            cancellationTokenSource.Cancel();
            await Task.WhenAll(receiveTask, sendTask);
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
            Disconnect();
        }
    }

    private async Task ReceiveDataAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];

        try
        {
            while (isConnected && !cancellationToken.IsCancellationRequested)
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead > 0)
                    {
                        ProcessReceivedData(buffer, bytesRead);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            if (isConnected)
            {
                Console.WriteLine($"\nChyba při příjmu: {ex.Message}");
            }
        }
    }

    private void ProcessReceivedData(byte[] data, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (data[i] == IAC && i + 2 < length)
            {
                // Zpracování Telnet příkazů
                ProcessTelnetCommand(data[i + 1], data[i + 2]);
                i += 2; // Přeskočit zpracované bajty
            }
            else
            {
                // Normální data - vypsat na konzoli
                char character = (char)data[i];
                Console.Write(character);
            }
        }
    }

    private void ProcessTelnetCommand(byte command, byte option)
    {
        // Zde lze implementovat reakce na Telnet option negotiation
        // Pro základního klienta většinu příkazů ignorujeme

        byte[] response = null;

        switch (command)
        {
            case DO:
                if (option == ECHO)
                {
                    // Server chce zapnout echo - souhlasíme
                    response = new byte[] { IAC, WILL, ECHO };
                }
                else
                {
                    // Ostatní option nepodporujeme
                    response = new byte[] { IAC, WONT, option };
                }
                break;

            case WILL:
                // Server nabízí option - většinou odmítneme
                response = new byte[] { IAC, DONT, option };
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

    private async Task SendDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (isConnected && !cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    string input = ReadLineWithCancel();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    if (input.Trim().ToUpper() == "QUIT")
                    {
                        Console.WriteLine("Ukončování spojení...");
                        break;
                    }
                    else if (input.Trim().ToUpper() == "CLEAR")
                    {
                        Console.Clear();
                        continue;
                    }

                    await SendStringAsync(input + "\r\n", cancellationToken);
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
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

    private async Task SendStringAsync(string text, CancellationToken cancellationToken)
    {
        byte[] data = Encoding.UTF8.GetBytes(text);
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private string ReadLineWithCancel()
    {
        StringBuilder input = new StringBuilder();

        while (true)
        {
            var keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return input.ToString();

                case ConsoleKey.Escape:
                case ConsoleKey.C when (keyInfo.Modifiers & ConsoleModifiers.Control) != 0:
                    Console.WriteLine("\nUkončování spojení...");
                    cancellationTokenSource?.Cancel();
                    return "QUIT";

                case ConsoleKey.Backspace when input.Length > 0:
                    input.Remove(input.Length - 1, 1);
                    Console.Write("\b \b");
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
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        input.Append(keyInfo.KeyChar);
                        Console.Write(keyInfo.KeyChar);
                    }
                    break;
            }
        }
    }

    private void Disconnect()
    {
        isConnected = false;

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