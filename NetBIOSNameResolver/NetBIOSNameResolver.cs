using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetBIOSNameResolver
{
    class NetBIOSNameResolver
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Získání NetBIOS jména z IP adresy ===\n");

            // Kontrola počtu argumentů
            if (args.Length != 1)
            {
                Console.WriteLine("Použití: NetBIOSNameResolver <IPv4 adresa>");
                return;  // Ukončení programu při chybném počtu argumentů
            }

            string ipAddressString = args[0];  // Získání URL z prvního argumentu

            if (!IPAddress.TryParse(ipAddressString, out IPAddress ipAddress))
            {
                Console.WriteLine("Neplatná IP adresa.");
                return;
            }

            Console.WriteLine($"Zpracovávám IP: {ipAddressString}\n");

            // 1. Metoda pomocí nbtstat (jednoduchá)
            string nbtstatName = await GetNetBIOSNameViaNbtstatAsync(ipAddressString);
            //string nbtstatName = GetNetBIOSNameViaNbtstat(ipAddressString);

            Console.WriteLine($"nbtstat metoda: {(string.IsNullOrEmpty(nbtstatName) ? "Nenalezeno" : nbtstatName)}");

            // 2. Přímá UDP metoda (port 137) - vylepšená verze
            string udpName = GetNetBIOSNameViaUDP(ipAddress);
            Console.WriteLine($"UDP metoda: {(string.IsNullOrEmpty(udpName) ? "Nenalezeno" : udpName)}");

            // 3. Metoda pomocí DNS reverzního dotazu (jako doplněk)
            string dnsName = await GetDNSNameAsync(ipAddress);
            Console.WriteLine($"DNS jméno: {(string.IsNullOrEmpty(dnsName) ? "Nenalezeno" : dnsName)}");

        }

        /// <summary>
        /// Získá NetBIOS jméno pomocí nbtstat -A (synchronní verze pro .NET Framework)
        /// </summary>
        static string GetNetBIOSNameViaNbtstat(string ipAddress)
        {
            try
            {
                string nbtstatPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", "nbtstat.exe");

                // Pokud 'Sysnative' neexistuje (např. na 32bit systému), použijeme standardní 'System32'
                if (!File.Exists(nbtstatPath))
                {
                    nbtstatPath = Path.Combine(Environment.SystemDirectory, "nbtstat.exe");
                    
                    if (!File.Exists(nbtstatPath))
                    {
                        return "Nbtstat nebyl nalezen v systému.";
                    }
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = nbtstatPath,
                    Arguments = $"-A {ipAddress}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(852)
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();

                    foreach (string line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Contains("<00>") && line.Contains("UNIQUE"))
                        {
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return parts.Length > 0 ? parts[0].Trim() : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Asynchronní čekání na dokončení procesu pro starší verze .NET
        /// </summary>
        static Task WaitForExitAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>();

            void ProcessExited(object sender, EventArgs e)
            {
                tcs.TrySetResult(true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += ProcessExited;

            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                if (process.HasExited)
                {
                    tcs.TrySetResult(true);
                }
                return tcs.Task;
            }
        }

        /// <summary>
        /// Asynchronní verze metody nbtstat
        /// </summary>
        static async Task<string> GetNetBIOSNameViaNbtstatAsync(string ipAddress)
        {
            try
            {
                string nbtstatPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", "nbtstat.exe");

                // Pokud 'Sysnative' neexistuje (např. na 32bit systému), použijeme standardní 'System32'
                if (!File.Exists(nbtstatPath))
                {
                    nbtstatPath = Path.Combine(Environment.SystemDirectory, "nbtstat.exe");

                    if (!File.Exists(nbtstatPath))
                    {
                        return "Nbtstat nebyl nalezen v systému.";
                    }
                }

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = nbtstatPath,
                    Arguments = $"-A {ipAddress}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(852)
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    // Asynchronní čekání pomocí vlastní implementace
                    await WaitForExitAsync(process);

                    string output = process.StandardOutput.ReadToEnd();

                    foreach (string line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Contains("<00>") && line.Contains("UNIQUE"))
                        {
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 1)
                            {
                                string potentialName = parts[0].Trim();
                                if (!string.IsNullOrEmpty(potentialName) && potentialName.Length <= 15)
                                {
                                    return potentialName;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při nbtstat: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Získá NetBIOS jméno přímým UDP dotazem na port 137 (vylepšená verze)
        /// </summary>
        static string GetNetBIOSNameViaUDP(IPAddress targetAddress)
        {
            // NetBIOS jméno může být v odpovědi na různých pozicích
            byte[] nameRequest = new byte[] {
                0x80, 0x94, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x43, 0x4b, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x00, 0x00,
                0x21, 0x00, 0x01
            };

            byte[] receiveBuffer = new byte[1024];
            using (Socket requestSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                requestSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
                EndPoint remoteEndpoint = new IPEndPoint(targetAddress, 137);
                IPEndPoint originEndpoint = new IPEndPoint(IPAddress.Any, 0);
                requestSocket.Bind(originEndpoint);
                requestSocket.SendTo(nameRequest, remoteEndpoint);

                try
                {
                    int receivedByteCount = requestSocket.ReceiveFrom(receiveBuffer, ref remoteEndpoint);
                    if (receivedByteCount >= 90)
                    {
                        Encoding enc = Encoding.ASCII;

                        // Pokus o extrakci NetBIOS jména z různých pozic
                        string[] possibleNames = {
                            enc.GetString(receiveBuffer, 57, 15).Trim(),
                            enc.GetString(receiveBuffer, 59, 15).Trim(),
                            enc.GetString(receiveBuffer, 61, 15).Trim(),
                            enc.GetString(receiveBuffer, 75, 15).Trim(),
                            enc.GetString(receiveBuffer, 79, 15).Trim(),
                            enc.GetString(receiveBuffer, 81, 15).Trim()
                        };

                        foreach (string name in possibleNames)
                        {
                            if (!string.IsNullOrEmpty(name) && name.Length <= 15 && !name.Contains('\0'))
                            {
                                return name;
                            }
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket chyba při UDP komunikaci: {ex.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// Získá DNS jméno jako doplňkovou informaci
        /// </summary>
        static async Task<string> GetDNSNameAsync(IPAddress ipAddress)
        {
            try
            {
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                return hostEntry.HostName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při DNS dotazu: {ex.Message}");
                return null;
            }
        }
    }
}