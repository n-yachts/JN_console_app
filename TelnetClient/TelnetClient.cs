using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class TelnetClient
{
    private TcpClient client;
    private NetworkStream stream;
    private CancellationTokenSource cancellationTokenSource;
    private bool isConnected = false;

    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Použití: TelnetClient <hostname> <port>");
            Console.WriteLine("Příklad: TelnetClient localhost 23");
            return;
        }

        string hostname = args[0];
        int port = int.Parse(args[1]);

        var telnetClient = new TelnetClient();
        await telnetClient.ConnectAsync(hostname, port);
    }

    public async Task ConnectAsync(string hostname, int port)
    {
        try
        {
            cancellationTokenSource = new CancellationTokenSource();
            client = new TcpClient();

            Console.WriteLine($"Připojování k {hostname}:{port}...");

            await client.ConnectAsync(hostname, port);
            stream = client.GetStream();
            isConnected = true;

            Console.WriteLine($"Připojeno k {hostname}:{port}");
            Console.WriteLine("Pro ukončení napište 'QUIT' nebo stiskněte Ctrl+C\n");

            // Spustíme úlohy pro čtení a zápis
            var receiveTask = Task.Run(() => ReceiveDataAsync(cancellationTokenSource.Token));
            var sendTask = Task.Run(() => SendDataAsync(cancellationTokenSource.Token));

            // Čekáme na dokončení kterékoliv úlohy
            await Task.WhenAny(receiveTask, sendTask);

            cancellationTokenSource.Cancel();
            await Task.WhenAll(receiveTask, sendTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při připojování: {ex.Message}");
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
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.Write(data);
                    }
                }
                else
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Očekávané při zrušení
        }
        catch (Exception ex)
        {
            if (isConnected)
            {
                Console.WriteLine($"\nChyba při příjmu dat: {ex.Message}");
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

                    byte[] data = Encoding.UTF8.GetBytes(input + "\r\n");
                    await stream.WriteAsync(data, 0, data.Length, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }
                else
                {
                    await Task.Delay(50, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Očekávané při zrušení
        }
        catch (Exception ex)
        {
            if (isConnected)
            {
                Console.WriteLine($"\nChyba při odesílání dat: {ex.Message}");
            }
        }
    }

    private string ReadLineWithCancel()
    {
        StringBuilder input = new StringBuilder();
        ConsoleKeyInfo keyInfo;

        while (true)
        {
            keyInfo = Console.ReadKey(true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input.ToString();
            }
            else if (keyInfo.Key == ConsoleKey.Escape ||
                    (keyInfo.Key == ConsoleKey.C && (keyInfo.Modifiers & ConsoleModifiers.Control) != 0))
            {
                Console.WriteLine("\nUkončování...");
                cancellationTokenSource?.Cancel();
                return "QUIT";
            }
            else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input.Remove(input.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                input.Append(keyInfo.KeyChar);
                Console.Write(keyInfo.KeyChar);
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
            cancellationTokenSource?.Cancel();
        }
        catch
        {
            // Ignorovat chyby při zavírání
        }

        Console.WriteLine("\nSpojení ukončeno.");
    }
}