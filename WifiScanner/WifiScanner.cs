using System;  // Import základních systémových knihoven
using System.Collections.Generic;  // Import knihovny pro práci s kolekcemi (List, atd.)
using System.Diagnostics;  // Import pro práci s procesy (spouštění příkazů)
using System.Security.Principal;
using System.Text;  // Import pro práci s kódováním textu

class WifiScanner  // Hlavní třída programu
{
    static void Main()  // Hlavní vstupní bod programu
    {
        // Nastavení kódování konzole na UTF-8
        Console.OutputEncoding = Encoding.UTF8;
        
        Console.WriteLine("Wireless Network Scanner\n");  // Výpis nadpisu programu

        // Kontrola administrátorských oprávnění
        if (!IsRunningAsAdmin())
        {
            Console.WriteLine("⚠️  UPOZORNĚNÍ: Program není spuštěn s administrátorskými oprávněními!");
            Console.WriteLine("Pro správnou funkci skenování WiFi sítí spusťte program jako správce (Run as Administrator).");
            Console.WriteLine("\nStiskněte libovolnou klávesu pro ukončení...");
            Console.ReadKey();
            return;
        }

        ScanWindowsWifi();  // Spuštění Windows-specifického skenování
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
                StandardOutputEncoding = Encoding.GetEncoding(1250) // Windows-1250 pro české znaky
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
            else if (trimmed.StartsWith("Signal") && currentNetwork != null)  // Úroveň signálu
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.Signal = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce síly signálu
            }
            else if (trimmed.StartsWith("Type") && currentNetwork != null)  // Typ zabezpečení
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.AuthType = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce autentizace
            }
            else if (trimmed.StartsWith("Channel") && currentNetwork != null)  // Číslo kanálu
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.Channel = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce kanálu
            }
            else if (trimmed.StartsWith("BSSID") && currentNetwork != null)  // MAC adresa přístupového bodu
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.BSSID = trimmed.Substring(colonIndex + 1).Trim();  // Extrakce BSSID
            }
        }

        if (currentNetwork != null)  // Přidání poslední sítě
            networks.Add(currentNetwork);

        // Výpis výsledků
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
Skenování na Windows:
 Spouští systémový příkaz netsh wlan show networks mode=bssid
 Zachytává a parsuje výstup s informacemi o WiFi sítích
 Používá kódování Windows-1250 pro české znaky
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