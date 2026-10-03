using System;
using System.Diagnostics;  // Spuštění externího programu nbtstat (Process)
using System.IO;  // Práce se soubory a cestami (File, Path)
using System.Net;  // Třídy pro práci se sítí (IPAddress, IPEndPoint, Dns)
using System.Net.Sockets;  // UDP socket pro přímý dotaz na port 137
using System.Text;  // Kódování textu (Encoding)
using System.Threading;  // CancellationToken pro vlastní WaitForExitAsync
using System.Threading.Tasks;  // Asynchronní programování (Task, async/await)

namespace NetBIOSNameResolver
{
    class NetBIOSNameResolver  // Zjišťuje NetBIOS jméno počítače z IP adresy třemi způsoby
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

            string ipAddressString = args[0];  // Získání IP adresy z prvního argumentu

            if (!IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) ||
                ipAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                Console.WriteLine("Neplatná IPv4 adresa.");
                return;
            }

            Console.WriteLine($"Zpracovávám IP: {ipAddressString}\n");

            // 1. Metoda pomocí nbtstat (jednoduchá)
            string nbtstatName = await GetNetBIOSNameViaNbtstatAsync(ipAddressString);

            Console.WriteLine($"nbtstat metoda: {(string.IsNullOrEmpty(nbtstatName) ? "Nenalezeno" : nbtstatName)}");

            // 2. Přímý dotaz Node Status na UDP port 137
            string udpName = GetNetBIOSNameViaUDP(ipAddress);
            Console.WriteLine($"UDP metoda: {(string.IsNullOrEmpty(udpName) ? "Nenalezeno" : udpName)}");

            // 3. Metoda pomocí DNS reverzního dotazu (jako doplněk)
            string dnsName = await GetDNSNameAsync(ipAddress);
            Console.WriteLine($"DNS jméno: {(string.IsNullOrEmpty(dnsName) ? "Nenalezeno" : dnsName)}");

        }

        /// <summary>
        /// Asynchronní čekání na dokončení procesu pro starší verze .NET
        /// </summary>
        static Task WaitForExitAsync(System.Diagnostics.Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>();

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => tcs.TrySetResult(true);

            // Registrace zrušení musí žít do dokončení tasku, proto se neuvolňuje hned (žádný using)
            cancellationToken.Register(() => tcs.TrySetCanceled());

            // Proces mohl skončit ještě před registrací události
            if (process.HasExited)
            {
                tcs.TrySetResult(true);
            }

            return tcs.Task;
        }

        /// <summary>
        /// Asynchronní verze metody nbtstat
        /// </summary>
        static async Task<string> GetNetBIOSNameViaNbtstatAsync(string ipAddress)
        {
            try
            {
                string nbtstatPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative", "nbtstat.exe");

                // Sysnative zpřístupní 64bitový System32 ze 32bitového procesu; pokud neexistuje, použijeme standardní System32
                if (!File.Exists(nbtstatPath))
                {
                    nbtstatPath = Path.Combine(Environment.SystemDirectory, "nbtstat.exe");

                    if (!File.Exists(nbtstatPath))
                    {
                        Console.WriteLine("Nbtstat nebyl nalezen v systému.");
                        return null;
                    }
                }

                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = nbtstatPath,
                    Arguments = $"-A {ipAddress}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(852)  // OEM kódová stránka české konzole
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    // Asynchronní čekání pomocí vlastní implementace
                    await WaitForExitAsync(process);

                    string output = process.StandardOutput.ReadToEnd();  // Výstup nbtstat (tabulka jmen)

                    foreach (string line in output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // Řádek s unikátním jménem typu <00> (Workstation) obsahuje jméno počítače na začátku
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
        /// Získá NetBIOS jméno přímým UDP dotazem (NBSTAT / Node Status) na port 137
        /// </summary>
        static string GetNetBIOSNameViaUDP(IPAddress targetAddress)
        {
            // Dotaz Node Status: hlavička (ID 0x8094, 1 otázka), zakódované wildcard jméno "*" (CKAAAA...), typ NBSTAT (0x21), třída IN
            byte[] nameRequest = new byte[] {
                0x80, 0x94, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x43, 0x4b, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41,
                0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x41, 0x00, 0x00,
                0x21, 0x00, 0x01
            };

            byte[] receiveBuffer = new byte[1024];
            using (Socket requestSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                requestSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);  // Čekání na odpověď max. 5 s
                EndPoint remoteEndpoint = new IPEndPoint(targetAddress, 137);  // NetBIOS Name Service běží na UDP 137
                IPEndPoint originEndpoint = new IPEndPoint(IPAddress.Any, 0);
                requestSocket.Bind(originEndpoint);

                try
                {
                    requestSocket.SendTo(nameRequest, remoteEndpoint);

                    int receivedByteCount = requestSocket.ReceiveFrom(receiveBuffer, ref remoteEndpoint);

                    // Odpověď Node Status: 12 B hlavička, 34 B kódované jméno, 10 B (typ, třída, TTL, délka),
                    // poté počet jmen (1 B) a záznamy po 18 B (15 znaků jméno, 1 B typ, 2 B příznaky)
                    const int nameCountOffset = 56;
                    if (receivedByteCount > nameCountOffset)
                    {
                        int nameCount = receiveBuffer[nameCountOffset];
                        int offset = nameCountOffset + 1;

                        // Hledá se unikátní jméno typu <00> (Workstation); jinak první jméno v seznamu
                        string firstName = null;
                        for (int i = 0; i < nameCount && offset + 18 <= receivedByteCount; i++, offset += 18)
                        {
                            string name = Encoding.ASCII.GetString(receiveBuffer, offset, 15).Trim();
                            byte type = receiveBuffer[offset + 15];
                            bool isGroup = (receiveBuffer[offset + 16] & 0x80) != 0;

                            if (firstName == null && !string.IsNullOrEmpty(name))
                                firstName = name;

                            if (type == 0x00 && !isGroup && !string.IsNullOrEmpty(name))
                                return name;
                        }

                        return firstName;
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
/*
NetBIOS jméno z IP adresy:
 1. nbtstat -A <IP> - systémový nástroj Windows, program hledá řádek s unikátním jménem typu <00>
 2. Přímý dotaz NBSTAT (Node Status) na UDP port 137 - odpověď obsahuje počet jmen a záznamy po 18 bajtech
    (15 znaků jméno, 1 bajt typ služby, 2 bajty příznaky)
 3. Reverzní DNS dotaz jako doplněk
Poznámky:
 Zařízení musí mít zapnutý NetBIOS over TCP/IP a povolený UDP port 137
 Odpověď má smysl jen pro zařízení v lokální síti (Windows, Samba, některé tiskárny)
*/
