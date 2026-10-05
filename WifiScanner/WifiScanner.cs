using System;  // Import základních systémových knihoven
using System.Collections.Generic;  // Import knihovny pro práci s kolekcemi (List, atd.)
using System.Diagnostics;  // Import pro práci s procesy (spouštění příkazů)
using System.Security.Principal;  // Kontrola, zda program běží jako správce (WindowsIdentity)
using System.Text;  // Import pro práci s kódováním textu

class WifiScanner  // Hlavní třída programu
{
    static void Main()  // Hlavní vstupní bod programu
    {
        // Kódové stránky (např. OEM 852 u netsh) jsou v .NET dostupné až po registraci poskytovatele
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Nastavení kódování konzole na UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        
        Console.WriteLine("Wireless Network Scanner\n");  // Výpis nadpisu programu

        // netsh wlan show networks admina nevyžaduje (na Windows 11 24H2 ale potřebuje zapnuté polohové služby),
        // proto se při chybějících oprávněních jen upozorní a skenování se přesto spustí
        if (OperatingSystem.IsWindows() && !IsRunningAsAdmin())
        {
            Console.WriteLine("⚠️  Program není spuštěn jako správce. Pokud skenování selže, spusťte jej jako správce");
            Console.WriteLine("    a zkontrolujte, zda jsou zapnuté polohové služby.\n");
        }

        // Skenování se liší podle systému: Windows používá netsh, Linux nmcli (NetworkManager)
        if (OperatingSystem.IsWindows())
            ScanWindowsWifi();
        else if (OperatingSystem.IsLinux())
            ScanLinuxWifi();
        else
            Console.WriteLine("Tento systém není podporován (jen Windows a Linux).");
    }

    // Metoda pro kontrolu administrátorských oprávnění
    static bool IsRunningAsAdmin()
    {
        try
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    static void ScanWindowsWifi()  // Hlavní metoda pro skenování WiFi na Windows
    {
        try  // Zachycení možných chyb
        {
            // Příprava konfigurace pro spuštění externího procesu
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "netsh",  // Název spouštěného programu
                Arguments = "wlan show networks mode=bssid",  // Parametry příkazu
                RedirectStandardOutput = true,  // Přesměrování standardního výstupu
                RedirectStandardError = true,  // Přesměrování chybového výstupu
                UseShellExecute = false,  // Zakázání shellu pro přímé spuštění
                CreateNoWindow = true,  // Skrytí konzolového okna
                // netsh vypisuje v OEM kódové stránce konzole (v češtině 852), ne ve Windows-1250
                StandardOutputEncoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage)
            };

            using (Process process = new Process { StartInfo = startInfo })  // Vytvoření procesu
            {
                process.Start();  // Spuštění procesu
                string output = process.StandardOutput.ReadToEnd();  // Načtení celého výstupu
                string error = process.StandardError.ReadToEnd();  // Načtení chybového výstupu
                process.WaitForExit();  // Čekání na ukončení procesu

                if (!string.IsNullOrEmpty(output))  // Pokud byl nějaký výstup
                {
                    ParseNetshOutput(output);  // Zpracování výstupu
                }

                if (!string.IsNullOrEmpty(error))  // Pokud byly nějaké chyby
                {
                    Console.WriteLine($"Chyba: {error}");  // Výpis chyby
                }
            }
        }
        catch (Exception ex)  // Zachycení výjimky
        {
            Console.WriteLine($"Chyba při skenování WiFi: {ex.Message}");  // Výpis chyby
        }
    }

    static void ParseNetshOutput(string output)  // Zpracování výstupu z netsh
    {
        string[] lines = output.Split('\n');  // Rozdělení výstupu na řádky
        List<WifiNetwork> networks = new List<WifiNetwork>();  // Seznam pro ukládání sítí
        WifiNetwork currentNetwork = null;  // Reference na právě zpracovávanou síť

        foreach (string line in lines)  // Cyklus přes všechny řádky
        {
            string trimmed = line.Trim();  // Oříznutí bílých znaků

            if (trimmed.StartsWith("SSID"))  // Začátek nové sítě
            {
                if (currentNetwork != null)  // Pokud již máme nějakou síť
                    networks.Add(currentNetwork);  // Uložení předchozí sítě

                currentNetwork = new WifiNetwork();  // Vytvoření nové sítě
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.SSID = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce názvu sítě
            }
            // Názvy položek netsh jsou lokalizované, proto se hledá anglická i česká varianta
            else if ((trimmed.StartsWith("Signal") || trimmed.StartsWith("Signál")) && currentNetwork != null)  // Úroveň signálu
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.Signal = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce síly signálu
            }
            else if ((trimmed.StartsWith("Authentication") || trimmed.StartsWith("Ověř")) && currentNetwork != null)  // Typ zabezpečení
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.AuthType = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce autentizace
            }
            else if ((trimmed.StartsWith("Channel") || trimmed.StartsWith("Kanál")) && currentNetwork != null)  // Číslo kanálu
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.Channel = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce kanálu
            }
            else if (trimmed.StartsWith("BSSID") && currentNetwork != null)  // MAC adresa přístupového bodu
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.BSSID = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce BSSID (má-li síť více přístupových bodů, uloží se poslední)
            }
        }

        if (currentNetwork != null)  // Přidání poslední sítě
            networks.Add(currentNetwork);

        PrintNetworks(networks);
    }

    // Linux: nmcli v "terse" režimu vypisuje pole oddělená dvojtečkou (dvojtečka a zpětné lomítko v hodnotách jsou escapovány zpětným lomítkem)
    static void ScanLinuxWifi()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "nmcli",
                Arguments = "-t -f SSID,SIGNAL,CHAN,SECURITY,BSSID dev wifi list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"Chyba: {(string.IsNullOrWhiteSpace(error) ? "nmcli skončilo s kódem " + process.ExitCode : error.Trim())}");
                    return;
                }

                PrintNetworks(ParseNmcliOutput(output));
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Console.WriteLine("Příkaz nmcli nebyl nalezen. Nainstalujte NetworkManager (např. balíček network-manager).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při skenování WiFi: {ex.Message}");
        }
    }

    static List<WifiNetwork> ParseNmcliOutput(string output)  // Zpracování výstupu z nmcli
    {
        List<WifiNetwork> networks = new List<WifiNetwork>();

        foreach (string line in output.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            List<string> fields = SplitEscaped(line.TrimEnd('\r'));
            if (fields.Count < 5)
                continue;

            networks.Add(new WifiNetwork
            {
                SSID = fields[0].Length == 0 ? "(skrytá síť)" : fields[0],
                Signal = fields[1] + "%",
                Channel = fields[2],
                AuthType = fields[3].Length == 0 || fields[3] == "--" ? "Open (bez zabezpečení)" : fields[3],
                BSSID = fields[4]
            });
        }

        return networks;
    }

    // Rozdělí řádek na pole podle neescapovaných dvojteček a odstraní escapování (\: a \\)
    static List<string> SplitEscaped(string line)
    {
        List<string> fields = new List<string>();
        StringBuilder current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\\' && i + 1 < line.Length)
            {
                current.Append(line[++i]);  // Escapovaný znak se vezme doslova
            }
            else if (c == ':')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    static void PrintNetworks(List<WifiNetwork> networks)  // Výpis seznamu sítí
    {
        Console.WriteLine($"Nalezeno {networks.Count} WiFi sítí:\n");

        foreach (var network in networks)  // Cyklus přes všechny nalezené sítě
        {
            if (!string.IsNullOrEmpty(network.SSID) && network.SSID != " ")  // Filtrování prázdných SSID
            {
                Console.WriteLine($"📶 {network.SSID}");  // Výpis názvu sítě
                if (!string.IsNullOrEmpty(network.Signal))
                    Console.WriteLine($"   Signál: {network.Signal}");  // Výpis síly signálu
                if (!string.IsNullOrEmpty(network.AuthType))
                    Console.WriteLine($"   Autentizace: {network.AuthType}");  // Výpis typu zabezpečení
                if (!string.IsNullOrEmpty(network.Channel))
                    Console.WriteLine($"   Kanál: {network.Channel}");  // Výpis kanálu
                if (!string.IsNullOrEmpty(network.BSSID))
                    Console.WriteLine($"   BSSID: {network.BSSID}");  // Výpis MAC adresy
                Console.WriteLine();  // Prázdný řádek pro oddělení
            }
        }
    }
}

class WifiNetwork  // Třída pro reprezentaci WiFi sítě
{
    public string SSID { get; set; }  // Název sítě
    public string Signal { get; set; }  // Síla signálu
    public string AuthType { get; set; }  // Typ zabezpečení
    public string Channel { get; set; }  // Číslo kanálu
    public string BSSID { get; set; }  // MAC adresa přístupového bodu
}

/*
Skenování na Linuxu:
 Spouští příkaz nmcli -t -f SSID,SIGNAL,CHAN,SECURITY,BSSID dev wifi list (vyžaduje NetworkManager, root není potřeba)
Skenování na Windows:
 Spouští systémový příkaz netsh wlan show networks mode=bssid
 Zachytává a parsuje výstup s informacemi o WiFi sítích
 Čte výstup v OEM kódové stránce konzole (v češtině 852); názvy položek hledá česky i anglicky
Zpracování výstupu:
 Analyzuje řádek po řádku
 Identifikuje klíčové informace (SSID, signál, kanál, atd.)
 Vytváří objekty sítí a vypisuje je formátovaným způsobem
Zpracování chyb:
 Zachytává výjimky při spouštění procesů
 Zobrazuje uživatelsky přívětivé chybové zprávy
Výstup:
 Formátovaný seznam všech dostupných WiFi sítí s podrobnými informacemi

Pro každou síť zobrazí název, sílu signálu, typ zabezpečení, kanál a BSSID
*/