using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

class TelnetServer
{
    private TcpListener listener;
    private CancellationTokenSource cancellationTokenSource;
    private bool isRunning = false;
    private Dictionary<string, CommandHandler> commands;
    private ServerConfiguration configuration;

    public TelnetServer()
    {
        InitializeCommands();
        configuration = new ServerConfiguration();
    }

    static async Task Main(string[] args)
    {
        int port = 23;
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }

        var server = new TelnetServer();
        await server.StartAsync(port);
    }

    private void InitializeCommands()
    {
        commands = new Dictionary<string, CommandHandler>(StringComparer.OrdinalIgnoreCase)
        {
            { "show running-config", new CommandHandler("Zobrazí aktuální konfiguraci", ShowRunningConfig) },
            { "show version", new CommandHandler("Zobrazí verzi firmware", ShowVersion) },
            { "show interfaces", new CommandHandler("Zobrazí stav rozhraní", ShowInterfaces) },
            { "show arp", new CommandHandler("Zobrazí ARP tabulku", ShowArpTable) },
            { "enable", new CommandHandler("Přepne do privilegovaného módu", EnableMode) },
            { "disable", new CommandHandler("Přepne do uživatelského módu", DisableMode) },
            { "configure terminal", new CommandHandler("Přepne do konfiguračního módu", ConfigureTerminal) },
            { "hostname", new CommandHandler("Nastaví hostname zařízení", SetHostname) },
            { "exit", new CommandHandler("Ukončí aktuální mód", ExitMode) },
            { "help", new CommandHandler("Zobrazí nápovědu", ShowHelp) },
            { "?", new CommandHandler("Zobrazí nápovědu", ShowHelp) }
        };
    }

    public async Task StartAsync(int port = 23)
    {
        if (isRunning)
            return;

        cancellationTokenSource = new CancellationTokenSource();
        listener = new TcpListener(IPAddress.Any, port);

        try
        {
            listener.Start();
            isRunning = true;

            Console.WriteLine($"Telnet Server běží na portu {port}");
            Console.WriteLine("Stiskněte Ctrl+C pro ukončení serveru");
            Console.WriteLine("Dostupné příkazy: " + string.Join(", ", commands.Keys));

            // Registrace Ctrl+C handleru
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\nUkončování serveru...");
                cancellationTokenSource.Cancel();
            };

            await AcceptClientsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při spuštění serveru: {ex.Message}");
        }
        finally
        {
            Stop();
        }
    }

    private async Task AcceptClientsAsync()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(client, cancellationTokenSource.Token));
            }
            catch (ObjectDisposedException)
            {
                // Listener byl ukončen
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při přijímání klienta: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var clientEndPoint = client.Client.RemoteEndPoint;
        Console.WriteLine($"Nový klient připojen: {clientEndPoint}");

        using (client)
        using (var stream = client.GetStream())
        {
            var session = new ClientSession
            {
                Client = client,
                Stream = stream,
                IsPrivilegedMode = false,
                IsConfigMode = false,
                Hostname = configuration.DefaultHostname
            };

            try
            {
                await SendWelcomeMessage(session);

                byte[] buffer = new byte[1024];
                StringBuilder inputBuffer = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead == 0)
                            break;

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        inputBuffer.Append(receivedData);

                        // Zpracování příkazů po řádcích
                        if (receivedData.Contains("\n") || receivedData.Contains("\r"))
                        {
                            string input = inputBuffer.ToString().Trim();
                            inputBuffer.Clear();

                            if (!string.IsNullOrEmpty(input))
                            {
                                string response = await ProcessCommand(input, session);
                                await SendResponse(session, response);
                            }

                            // Zobrazení promptu
                            await SendPrompt(session);
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
                Console.WriteLine($"Chyba při komunikaci s klientem {clientEndPoint}: {ex.Message}");
            }
        }

        Console.WriteLine($"Klient odpojen: {clientEndPoint}");
    }

    private async Task SendWelcomeMessage(ClientSession session)
    {
        string welcomeMessage = $@"
Welcome to Network Device Simulator
Type 'help' or '?' for available commands

";
        await SendResponse(session, welcomeMessage);
        await SendPrompt(session);
    }

    private async Task SendPrompt(ClientSession session)
    {
        string prompt = session.IsConfigMode ? "(config)# "
                      : session.IsPrivilegedMode ? "# "
                      : "> ";

        string fullPrompt = $"{session.Hostname}{prompt}";
        await SendResponse(session, fullPrompt);
    }

    private async Task<string> ProcessCommand(string input, ClientSession session)
    {
        string command = input.Trim();

        if (commands.ContainsKey(command))
        {
            return await commands[command].Execute(session, configuration, command);
        }

        // Hledání podobných příkazů
        var similarCommands = commands.Keys.Where(c => c.StartsWith(command, StringComparison.OrdinalIgnoreCase)).ToList();
        if (similarCommands.Any())
        {
            return $"Neúplný příkaz. Možnosti: {string.Join(", ", similarCommands)}";
        }

        return $"Neznámý příkaz: '{command}'. Napište 'help' pro nápovědu.";
    }

    private async Task SendResponse(ClientSession session, string response)
    {
        byte[] data = Encoding.UTF8.GetBytes(response + "\r\n");
        await session.Stream.WriteAsync(data, 0, data.Length);
        await session.Stream.FlushAsync();
    }

    public void Stop()
    {
        isRunning = false;
        listener?.Stop();
        cancellationTokenSource?.Cancel();
        Console.WriteLine("Telnet Server byl ukončen.");
    }

    // Command Handlers
    private string ShowRunningConfig(ClientSession session, ServerConfiguration config, string command)
    {
        if (!session.IsPrivilegedMode)
            return "% Příkaz vyžaduje privilegovaný mód. Použijte 'enable'.";

        return $@"
Current configuration:
{config.GetConfigurationText(session.Hostname)}";
    }

    private string ShowVersion(ClientSession session, ServerConfiguration config, string command)
    {
        return $@"
Network Device Simulator Version 1.0
Firmware: Cisco IOS Software, Version 15.1(4)M12
Compiled: 2024-01-15
Memory: 256MB
";
    }

    private string ShowInterfaces(ClientSession session, ServerConfiguration config, string command)
    {
        return $@"
Interface                  Status        Protocol
GigabitEthernet0/0         up            up
GigabitEthernet0/1         down          down
GigabitEthernet0/2         up            up
Vlan1                      up            up
";
    }

    private string ShowArpTable(ClientSession session, ServerConfiguration config, string command)
    {
        if (!session.IsPrivilegedMode)
            return "% Příkaz vyžaduje privilegovaný mód. Použijte 'enable'.";

        return $@"
Protocol  Address          Age (min)  Hardware Addr   Type   Interface
Internet  192.168.1.1           5    0011.2233.4455  ARPA   GigabitEthernet0/0
Internet  192.168.1.10         12    aabb.ccdd.eeff  ARPA   GigabitEthernet0/0
Internet  192.168.1.100         -    0011.2233.4466  ARPA   GigabitEthernet0/2
";
    }

    private string EnableMode(ClientSession session, ServerConfiguration config, string command)
    {
        if (session.IsPrivilegedMode)
            return "Již jste v privilegovaném módu.";

        session.IsPrivilegedMode = true;
        return "";
    }

    private string DisableMode(ClientSession session, ServerConfiguration config, string command)
    {
        session.IsPrivilegedMode = false;
        session.IsConfigMode = false;
        return "";
    }

    private string ConfigureTerminal(ClientSession session, ServerConfiguration config, string command)
    {
        if (!session.IsPrivilegedMode)
            return "% Příkaz vyžaduje privilegovaný mód. Použijte 'enable'.";

        session.IsConfigMode = true;
        return "Enter configuration commands, one per line. End with CNTL/Z.";
    }

    private string SetHostname(ClientSession session, ServerConfiguration config, string command)
    {
        if (!session.IsConfigMode)
            return "% Příkaz vyžaduje konfigurační mód. Použijte 'configure terminal'.";

        string newHostname = command.Substring("hostname".Length).Trim();
        if (string.IsNullOrWhiteSpace(newHostname))
            return "Chybná syntaxe: hostname <name>";

        string oldHostname = session.Hostname;
        session.Hostname = newHostname;
        return $"{oldHostname} přejmenováno na {newHostname}";
    }

    private string ExitMode(ClientSession session, ServerConfiguration config, string command)
    {
        if (session.IsConfigMode)
        {
            session.IsConfigMode = false;
            return "";
        }
        else if (session.IsPrivilegedMode)
        {
            session.IsPrivilegedMode = false;
            return "";
        }

        return "exit";
    }

    private string ShowHelp(ClientSession session, ServerConfiguration config, string command)
    {
        var helpText = new StringBuilder();
        helpText.AppendLine("Dostupné příkazy:");

        foreach (var cmd in commands.OrderBy(c => c.Key))
        {
            helpText.AppendLine($"  {cmd.Key,-25} {cmd.Value.Description}");
        }

        return helpText.ToString();
    }
}

// Podpůrné třídy
public class CommandHandler
{
    public string Description { get; }
    private Func<ClientSession, ServerConfiguration, string, string> handler;

    public CommandHandler(string description, Func<ClientSession, ServerConfiguration, string, string> handler)
    {
        Description = description;
        this.handler = handler;
    }

    public Task<string> Execute(ClientSession session, ServerConfiguration config, string command)
    {
        return Task.FromResult(handler(session, config, command));
    }
}

public class ClientSession
{
    public TcpClient Client { get; set; }
    public NetworkStream Stream { get; set; }
    public bool IsPrivilegedMode { get; set; }
    public bool IsConfigMode { get; set; }
    public string Hostname { get; set; }
}

public class ServerConfiguration
{
    public string DefaultHostname { get; } = "Router";

    public string GetConfigurationText(string currentHostname)
    {
        return $@"
version 15.1
service timestamps debug datetime msec
service timestamps log datetime msec
hostname {currentHostname}
!
interface GigabitEthernet0/0
 ip address 192.168.1.1 255.255.255.0
 no shutdown
!
interface GigabitEthernet0/1
 shutdown
!
interface GigabitEthernet0/2
 ip address 10.0.0.1 255.255.255.0
 no shutdown
!
ip route 0.0.0.0 0.0.0.0 192.168.1.254
!
end
";
    }
}