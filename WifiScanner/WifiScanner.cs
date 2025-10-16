using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

class WifiScanner
{
    static void Main()
    {
        Console.WriteLine("Wireless Network Scanner\n");

        if (IsWindows())
        {
            ScanWindowsWifi();
        }
        else
        {
            Console.WriteLine("Tento program vyžaduje Windows pro plnou funkcionalitu.");
            ScanUsingNetsh();
        }
    }

    static bool IsWindows()
    {
        return Environment.OSVersion.Platform == PlatformID.Win32NT;
    }

    static void ScanWindowsWifi()
    {
        try
        {
            // Použití netsh příkazu pro získání WiFi sítí
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show networks mode=bssid",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(1250) // Windows-1250 pro češtinu
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    ParseNetshOutput(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Chyba: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při skenování WiFi: {ex.Message}");
        }
    }

    static void ScanUsingNetsh()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "nmcli",
                Arguments = "dev wifi",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine(output);
            }
        }
        catch
        {
            Console.WriteLine("Nelze spustit nmcli. Ujistěte se, že je nainstalován NetworkManager.");
        }
    }

    static void ParseNetshOutput(string output)
    {
        string[] lines = output.Split('\n');
        List<WifiNetwork> networks = new List<WifiNetwork>();
        WifiNetwork currentNetwork = null;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("SSID"))
            {
                if (currentNetwork != null)
                    networks.Add(currentNetwork);

                currentNetwork = new WifiNetwork();
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.SSID = trimmed.Substring(colonIndex + 1).Trim();
            }
            else if (trimmed.StartsWith("Signal") && currentNetwork != null)
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.Signal = trimmed.Substring(colonIndex + 1).Trim();
            }
            else if (trimmed.StartsWith("Type") && currentNetwork != null)
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.AuthType = trimmed.Substring(colonIndex + 1).Trim();
            }
            else if (trimmed.StartsWith("Channel") && currentNetwork != null)
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.Channel = trimmed.Substring(colonIndex + 1).Trim();
            }
            else if (trimmed.StartsWith("BSSID") && currentNetwork != null)
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                    currentNetwork.BSSID = trimmed.Substring(colonIndex + 1).Trim();
            }
        }

        if (currentNetwork != null)
            networks.Add(currentNetwork);

        // Výpis nalezených sítí
        Console.WriteLine($"Nalezeno {networks.Count} WiFi sítí:\n");

        foreach (var network in networks)
        {
            if (!string.IsNullOrEmpty(network.SSID) && network.SSID != " ")
            {
                Console.WriteLine($"📶 {network.SSID}");
                if (!string.IsNullOrEmpty(network.Signal))
                    Console.WriteLine($"   Signál: {network.Signal}");
                if (!string.IsNullOrEmpty(network.AuthType))
                    Console.WriteLine($"   Autentizace: {network.AuthType}");
                if (!string.IsNullOrEmpty(network.Channel))
                    Console.WriteLine($"   Kanál: {network.Channel}");
                if (!string.IsNullOrEmpty(network.BSSID))
                    Console.WriteLine($"   BSSID: {network.BSSID}");
                Console.WriteLine();
            }
        }
    }
}

class WifiNetwork
{
    public string SSID { get; set; }
    public string Signal { get; set; }
    public string AuthType { get; set; }
    public string Channel { get; set; }
    public string BSSID { get; set; }
}