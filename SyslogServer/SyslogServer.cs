using System;  // Import základních systémových funkcí a tříd
using System.Net;  // Import síťových funkcí (IPEndPoint)
using System.Net.Sockets;  // Import UDP klienta pro příjem zpráv
using System.Text;  // Import práce s textovými kódováními
using System.Threading;  // Import CancellationTokenSource pro ukončení přes Ctrl+C
using System.Threading.Tasks;  // Import asynchronního programování

class SyslogServer  // Hlavní třída - jednoduchý syslog server (UDP)
{
    // Názvy závažnosti (severity) podle RFC 5424 - index odpovídá číslu 0-7
    static readonly string[] Severities =
    {
        "Emergency", "Alert", "Critical", "Error", "Warning", "Notice", "Informational", "Debug"
    };

    // Názvy zdrojů zpráv (facility) 0-23
    static readonly string[] Facilities =
    {
        "kern", "user", "mail", "daemon", "auth", "syslog", "lpr", "news",
        "uucp", "cron", "authpriv", "ftp", "ntp", "audit", "alert", "clock",
        "local0", "local1", "local2", "local3", "local4", "local5", "local6", "local7"
    };

    static readonly int[] Counts = new int[8];  // Počet přijatých zpráv podle závažnosti (do souhrnu při ukončení)
    static int unknownCount = 0;  // Počet zpráv bez platné hlavičky <PRI>

    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - oba jsou volitelné
        if (args.Length > 2)
        {
            Console.WriteLine("Použití: SyslogServer [port=514] [max_severity=7]");
            Console.WriteLine("Příklad: SyslogServer 514 4   (zobrazí jen Emergency až Warning)");
            return;
        }

        int port = 514;  // Standardní syslog port
        int maxSeverity = 7;  // Výchozí = zobrazit všechny zprávy

        if (args.Length > 0 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535))
        {
            Console.WriteLine("Chyba: port musí být číslo 1-65535.");
            return;
        }

        if (args.Length > 1 && (!int.TryParse(args[1], out maxSeverity) || maxSeverity < 0 || maxSeverity > 7))
        {
            Console.WriteLine("Chyba: závažnost musí být číslo 0-7 (0 = Emergency, 7 = Debug).");
            return;
        }

        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
            // Ctrl+C ukončí smyčku příjmu a program vypíše souhrn
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;  // Zabránění okamžitému ukončení procesu
                cts.Cancel();
            };

            try
            {
                using (UdpClient listener = new UdpClient(port))
                {
                    Console.WriteLine($"Syslog server naslouchá na UDP portu {port}");
                    Console.WriteLine($"Zobrazuji zprávy se závažností 0-{maxSeverity} ({Severities[maxSeverity]} a závažnější)");
                    Console.WriteLine("Stiskněte Ctrl+C pro ukončení.\n");

                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            UdpReceiveResult result = await listener.ReceiveAsync(cts.Token);
                            ProcessMessage(result.Buffer, result.RemoteEndPoint, maxSeverity);
                        }
                        catch (OperationCanceledException)
                        {
                            break;  // Ukončení na žádost uživatele
                        }
                        catch (SocketException ex)
                        {
                            // Např. ICMP "port unreachable" z předchozího odeslání - server pokračuje dál
                            Console.WriteLine($"Chyba při příjmu: {ex.Message}");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Nelze otevřít port {port}: {ex.Message}");
                return;
            }
        }

        PrintSummary();
    }

    // Zpracuje jednu přijatou zprávu: rozloží <PRI>, vyfiltruje podle závažnosti a vypíše
    static void ProcessMessage(byte[] buffer, IPEndPoint remote, int maxSeverity)
    {
        string text = Encoding.UTF8.GetString(buffer).TrimEnd('\r', '\n', '\0');

        int severity = -1;  // -1 = neznámá závažnost (chybí hlavička <PRI>)
        int facility = -1;
        string body = text;

        // Hlavička <PRI> má tvar <číslo>, kde číslo = facility * 8 + severity
        if (text.StartsWith("<"))
        {
            int end = text.IndexOf('>');
            if (end > 1 && end <= 4 && int.TryParse(text.Substring(1, end - 1), out int pri) && pri >= 0 && pri <= 191)
            {
                facility = pri / 8;
                severity = pri % 8;
                body = text.Substring(end + 1).TrimStart();
            }
        }

        if (severity >= 0)
        {
            Counts[severity]++;
            if (severity > maxSeverity)
                return;  // Zpráva je méně závažná než zvolený filtr
        }
        else
        {
            unknownCount++;
        }

        string severityName = severity >= 0 ? $"{Severities[severity]} ({severity})" : "neznámá";
        string facilityName = facility >= 0 ? Facilities[facility] : "?";

        ConsoleColor original = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ColorFor(severity);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {remote.Address} | {facilityName} | {severityName}");
            Console.ForegroundColor = original;
            Console.WriteLine($"    {body}");
        }
        finally
        {
            Console.ForegroundColor = original;
        }
    }

    // Barva podle závažnosti: červená (0-3), žlutá (4), azurová (5), šedá (6-7)
    static ConsoleColor ColorFor(int severity)
    {
        if (severity < 0) return ConsoleColor.Magenta;
        if (severity <= 3) return ConsoleColor.Red;
        if (severity == 4) return ConsoleColor.Yellow;
        if (severity == 5) return ConsoleColor.Cyan;
        return ConsoleColor.Gray;
    }

    // Souhrn přijatých zpráv podle závažnosti (včetně těch, které filtr skryl)
    static void PrintSummary()
    {
        Console.WriteLine("\nSouhrn přijatých zpráv:");
        for (int i = 0; i < Severities.Length; i++)
            Console.WriteLine($"  {i} {Severities[i],-14}: {Counts[i]}");
        if (unknownCount > 0)
            Console.WriteLine($"  bez hlavičky <PRI>   : {unknownCount}");
    }
}

/*
Struktura programu:
 Aplikace je jednoduchý syslog server: přijímá zprávy, které zařízení (např. Cisco směrovač nebo přepínač) odešle na UDP port 514
 Na Cisco zařízení se odesílání zapíná příkazem "logging host <IP adresa tohoto počítače>"
 Závažnost lze omezit příkazem "logging trap <úroveň>" přímo na zařízení, případně druhým parametrem tohoto programu
Klíčové komponenty:
 Main() - kontrola parametrů, smyčka příjmu a ukončení přes Ctrl+C
 ProcessMessage() - rozbor hlavičky <PRI>, filtrování a výpis
 PrintSummary() - souhrn počtu zpráv podle závažnosti
Hlavička <PRI>:
 Číslo v ostrých závorkách = facility * 8 + severity
 Severity: 0 Emergency, 1 Alert, 2 Critical, 3 Error, 4 Warning, 5 Notice, 6 Informational, 7 Debug
Síťová komunikace:
 Používá connectionless UDP protokol, asynchronní příjem
 Na portu 514 musí být povolený příchozí provoz ve firewallu
Výstupy:
 Čas přijetí, IP adresa odesílatele, facility a závažnost (barevně) a text zprávy
*/
