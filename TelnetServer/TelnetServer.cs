using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

// Hlavní třída Telnet serveru simulujícího síťové zařízení
class TelnetServer
{
    private TcpListener listener; // TCP listener pro příchozí spojení
    private CancellationTokenSource cancellationTokenSource; // Řízení ukončení vláken
    private bool isRunning = false; // Stav serveru
    private Dictionary<string, CommandHandler> commands; // Slovník dostupných příkazů
    private ServerConfiguration configuration; // Konfigurace serveru

    // Konstruktor inicializující příkazy a konfiguraci
    public TelnetServer()
    {
        InitializeCommands();
        configuration = new ServerConfiguration();
    }

    // Hlavní vstupní bod aplikace
    static async Task Main(string[] args)
    {
        int port = 23; // Výchozí telnet port
        // Zpracování argumentů příkazové řádky pro vlastní port
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }

        var server = new TelnetServer();
        await server.StartAsync(port); // Spuštění serveru
    }

    // Inicializace slovníku podporovaných příkazů
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

    // Hlavní metoda pro spuštění serveru
    public async Task StartAsync(int port = 23)
    {
        if (isRunning)
            return;

        cancellationTokenSource = new CancellationTokenSource();
        listener = new TcpListener(IPAddress.Any, port); // Naslouchá na všech rozhraních

        try
        {
            listener.Start();
            isRunning = true;

            Console.WriteLine($"Telnet Server běží na portu {port}");
            Console.WriteLine("Stiskněte Ctrl+C pro ukončení serveru");
            Console.WriteLine("Dostupné příkazy: " + string.Join(", ", commands.Keys));

            // Registrace obsluhy události Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Zabránit okamžitému ukončení
                Console.WriteLine("\nUkončování serveru...");
                cancellationTokenSource.Cancel();
            };

            await AcceptClientsAsync(); // Spuštění přijímání klientů
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při spuštění serveru: {ex.Message}");
        }
        finally
        {
            Stop(); // Zajištění úklidu prostředků
        }
    }

    // Smyčka pro přijímání nových klientů
    private async Task AcceptClientsAsync()
    {
        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(); // Čekání na klienta
                _ = Task.Run(() => HandleClientAsync(client, cancellationTokenSource.Token)); // Spuštění vlákna pro klienta
            }
            catch (ObjectDisposedException)
            {
                // Listener byl ukončen během čekání
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při přijímání klienta: {ex.Message}");
            }
        }
    }

    // Obsluha jednotlivého klienta
    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var clientEndPoint = client.Client.RemoteEndPoint;
        Console.WriteLine($"Nový klient připojen: {clientEndPoint}");

        using (client)
        using (var stream = client.GetStream())
        {
            // Vytvoření relace pro sledování stavu klienta
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
                await SendWelcomeMessage(session); // Odeslání úvodní zprávy

                byte[] buffer = new byte[1024]; // Buffer pro příchozí data
                StringBuilder inputBuffer = new StringBuilder(); // Buffer pro stavbu příkazů

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead == 0) // Klient se odpojil
                            break;

                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        inputBuffer.Append(receivedData);

                        // Zpracování po přijetí nového řádku
                        if (receivedData.Contains("\n") || receivedData.Contains("\r"))
                        {
                            string input = inputBuffer.ToString().Trim();
                            inputBuffer.Clear();

                            if (!string.IsNullOrEmpty(input))
                            {
                                string response = await ProcessCommand(input, session);
                                await SendResponse(session, response);
                            }

                            await SendPrompt(session); // Zobrazení nového promptu
                        }
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken); // Krátký spánek pro uvolnění CPU
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

    // Odeslání uvítací zprávy novému klientovi
    private async Task SendWelcomeMessage(ClientSession session)
    {
        string welcomeMessage = $@"
Welcome to Network Device Simulator
Type 'help' or '?' for available commands

";
        await SendResponse(session, welcomeMessage);
        await SendPrompt(session);
    }

    // Sestavení a odeslání promptu podle aktuálního módu
    private async Task SendPrompt(ClientSession session)
    {
        string prompt = session.IsConfigMode ? "(config)# "
                      : session.IsPrivilegedMode ? "# "
                      : "> ";

        string fullPrompt = $"{session.Hostname}{prompt}";
        await SendResponse(session, fullPrompt);
    }

    // Zpracování příkazu od klienta
    private async Task<string> ProcessCommand(string input, ClientSession session)
    {
        string command = input.Trim();

        if (commands.ContainsKey(command))
        {
            return await commands[command].Execute(session, configuration, command);
        }

        // Hledání podobných příkazů pro lepší UX
        var similarCommands = commands.Keys.Where(c => c.StartsWith(command, StringComparison.OrdinalIgnoreCase)).ToList();
        if (similarCommands.Any())
        {
            return $"Neúplný příkaz. Možnosti: {string.Join(", ", similarCommands)}";
        }

        return $"Neznámý příkaz: '{command}'. Napište 'help' pro nápovědu.";
    }

    // Odeslání odpovědi klientovi
    private async Task SendResponse(ClientSession session, string response)
    {
        byte[] data = Encoding.UTF8.GetBytes(response + "\r\n");
        await session.Stream.WriteAsync(data, 0, data.Length);
        await session.Stream.FlushAsync();
    }

    // Ukončení činnosti serveru
    public void Stop()
    {
        isRunning = false;
        listener?.Stop();
        cancellationTokenSource?.Cancel();
        Console.WriteLine("Telnet Server byl ukončen.");
    }

    // Příkaz: Zobrazení konfigurace
    private string ShowRunningConfig(ClientSession session, ServerConfiguration config, string command)
    {
        if (!session.IsPrivilegedMode)
            return "% Příkaz vyžaduje privilegovaný mód. Použijte 'enable'.";

        return $@"
Current configuration:
{config.GetConfigurationText(session.Hostname)}";
    }

    // Příkaz: Zobrazení verze
    private string ShowVersion(ClientSession session, ServerConfiguration config, string command)
    {
        return $@"
Network JN Device Simulator Version 1.0
Firmware: Cisco IOS Software, Herrysion 9 3/4 (version)
Compiled: 2025-10-20
Memory: 256MB
";
    }

    // Příkaz: Zobrazení rozhraní
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

    // Příkaz: Zobrazení ARP tabulky
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

    // Příkaz: Přepnutí do privilegovaného módu
    private string EnableMode(ClientSession session, ServerConfiguration config, string command)
    {
        if (session.IsPrivilegedMode)
            return "Již jste v privilegovaném módu.";

        session.IsPrivilegedMode = true;
        return "";
    }

    // Příkaz: Přepnutí do uživatelského módu
    private string DisableMode(ClientSession session, ServerConfiguration config, string command)
    {
        session.IsPrivilegedMode = false;
        session.IsConfigMode = false;
        return "";
    }

    // Příkaz: Přepnutí do konfiguračního módu
    private string ConfigureTerminal(ClientSession session, ServerConfiguration config, string command)
    {
        if (!session.IsPrivilegedMode)
            return "% Příkaz vyžaduje privilegovaný mód. Použijte 'enable'.";

        session.IsConfigMode = true;
        return "Enter configuration commands, one per line. End with CNTL/Z.";
    }

    // Příkaz: Nastavení hostname
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

    // Příkaz: Opuštění aktuálního módu
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

    // Příkaz: Zobrazení nápovědy
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

// Třída pro reprezentaci příkazu
public class CommandHandler
{
    public string Description { get; } // Popis příkazu pro nápovědu
    private Func<ClientSession, ServerConfiguration, string, string> handler; // Funkce pro provedení příkazu

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

// Třída pro sledování stavu relace klienta
public class ClientSession
{
    public TcpClient Client { get; set; } // TCP klient
    public NetworkStream Stream { get; set; } // Síťový stream
    public bool IsPrivilegedMode { get; set; } // Stav privilegovaného módu
    public bool IsConfigMode { get; set; } // Stav konfiguračního módu
    public string Hostname { get; set; } // Aktuální hostname
}

// Třída pro konfiguraci serveru
public class ServerConfiguration
{
    public string DefaultHostname { get; } = "Router"; // Výchozí hostname

    // Generování ukázkové konfigurace
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

/*
Telnet server simulující síťové zařízení (router/switch)
Podpora různých uživatelských módů (uživatelský, privilegovaný, konfigurační)
Příkazy inspirované Cisco IOS (show running-config, enable, configure terminal, atd.)
Kompletní zpracování TCP spojení s více klienty
Ošetření výjimek a korektní ukončování
Rozšiřitelná architektura pro přidávání nových příkazů
*/