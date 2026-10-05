using System;
using System.IO;  // File.ReadLines pro čtení /proc/net/arp na Linuxu
using System.Net;  // IPAddress pro převod IP adresy
using System.Net.NetworkInformation;  // NetworkInterface pro zjištění názvu rozhraní podle indexu
using System.Runtime.InteropServices;  // DllImport, Marshal, StructLayout - volání nativního Win32 API
using System.ComponentModel;  // Win32Exception - převod chybového kódu Windows na zprávu

class ARPTable  // Hlavní třída programu - výpis ARP tabulky systému (Windows přes Win32 API, Linux ze souboru /proc/net/arp)
{
    // Import Win32 API funkce pro získání ARP tabulky
    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetIpNetTable(IntPtr pIpNetTable, ref uint pdwSize, bool bOrder);

    // Import Win32 API funkce FreeMibTable (v této ukázce se nepoužívá, paměť se uvolňuje přes Marshal.FreeCoTaskMem)
    [DllImport("iphlpapi.dll")]
    static extern uint FreeMibTable(IntPtr plpNetTable);

    // Konstanty pro typy záznamů v ARP tabulce
    // (další hodnoty: 1 = other, 2 = invalid)
    const int MIB_IPNET_TYPE_DYNAMIC = 3;  // Dynamický záznam
    const int MIB_IPNET_TYPE_STATIC = 4;   // Statický záznam

    // Struktura reprezentující jeden záznam v ARP tabulce
    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETROW
    {
        public uint dwIndex;        // Index síťového rozhraní
        public uint dwPhysAddrLen;  // Délka MAC adresy
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] bPhysAddr;    // Pole pro MAC adresu (max 8 bytů)
        public uint dwAddr;         // IPv4 adresa uložená jako uint (bajty v pořadí, v jakém jsou v adrese)
        public uint dwType;         // Typ záznamu (dynamic/static)
    }

    // Hlavní struktura ARP tabulky (v kódu se nepoužívá, počet záznamů se čte přímo z paměti a záznamy se načítají postupně)
    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETTABLE
    {
        public uint dwNumEntries;   // Počet záznamů v tabulce
        public MIB_IPNETROW table;  // První záznam (ostatní následují v paměti za ním)
    }

    static void Main()
    {
        // Výpis hlavičky programu
        Console.WriteLine(OperatingSystem.IsWindows() ? "ARP Table - Lokální cache (Win32 API)\n" : "ARP Table - Lokální cache (/proc/net/arp)\n");

        // Formátování sloupců výpisu
        Console.WriteLine("{0,-15} {1,-17} {2,-8} {3}", "IP Address", "Physical Address", "Type", "Interface");
        Console.WriteLine(new string('-', 60));  // Oddělovací čára

        try
        {
            // Zavolání metody pro zobrazení ARP tabulky podle operačního systému
            if (OperatingSystem.IsWindows())
                DisplayARPTable();
            else if (OperatingSystem.IsLinux())
                DisplayLinuxArpTable();
            else
                Console.WriteLine("Tento systém není podporován (jen Windows a Linux).");
        }
        catch (Exception ex)
        {
            // Zachycení a výpis případných chyb
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    // Linux: jádro vystavuje ARP tabulku jako textový soubor (sloupce: IP, typ HW, příznaky, MAC, maska, rozhraní)
    static void DisplayLinuxArpTable()
    {
        foreach (string line in File.ReadLines("/proc/net/arp"))
        {
            string[] fields = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 6 || fields[0] == "IP")  // První řádek je záhlaví
                continue;

            int flags = Convert.ToInt32(fields[2], 16);  // Příznaky ve tvaru 0x2: 0x2 = úplný záznam, 0x4 = trvalý (statický)
            string type = (flags & 0x4) != 0 ? "static" : (flags & 0x2) != 0 ? "dynamic" : "incomplete";
            string mac = fields[3].ToUpperInvariant().Replace(':', '-');  // Stejný formát jako ve Windows

            Console.WriteLine($"{fields[0],-15} {mac,-17} {type,-8} {fields[5]}");
        }
    }

    static void DisplayARPTable()
    {
        uint bufferSize = 0;  // Proměnná pro velikost požadované paměti

        // První volání zjistí potřebnou velikost bufferu
        uint result = GetIpNetTable(IntPtr.Zero, ref bufferSize, false);

        // Kontrola chyby (očekáváme ERROR_INSUFFICIENT_BUFFER = 122)
        if (result != 122)
        {
            throw new Win32Exception((int)result);
        }

        // Alokace paměti pro ARP tabulku
        IntPtr buffer = Marshal.AllocCoTaskMem((int)bufferSize);

        try
        {
            // Druhé volání získává skutečná data
            result = GetIpNetTable(buffer, ref bufferSize, false);
            if (result != 0)  // 0 znamená úspěch
            {
                throw new Win32Exception((int)result);
            }

            // Získání ukazatele na začátek tabulky
            IntPtr currentEntry = buffer;

            // Přečtení počtu záznamů (první 4 byty)
            uint entriesCount = (uint)Marshal.ReadInt32(currentEntry);

            // Posun za hlavičku tabulky
            currentEntry += 4;

            // Cyklus přes všechny záznamy v ARP tabulce
            for (int i = 0; i < entriesCount; i++)
            {
                // Převedení nativní struktury na spravovanou strukturu
                MIB_IPNETROW arpEntry = (MIB_IPNETROW)Marshal.PtrToStructure(
                    currentEntry, typeof(MIB_IPNETROW));

                // Zobrazení jednoho záznamu
                DisplayARPEntry(arpEntry);

                // Posun na další záznam v paměti
                currentEntry += Marshal.SizeOf(typeof(MIB_IPNETROW));
            }
        }
        finally
        {
            // Uvolnění alokované paměti
            Marshal.FreeCoTaskMem(buffer);
        }
    }

    static void DisplayARPEntry(MIB_IPNETROW arpEntry)
    {
        // Převod IP adresy z uint na IPAddress objekt
        IPAddress ipAddress = new IPAddress(arpEntry.dwAddr);

        // Převod MAC adresy na formát string s pomlčkami
        string macAddress = BitConverter.ToString(
            arpEntry.bPhysAddr, 0, (int)arpEntry.dwPhysAddrLen);

        // Určení typu záznamu pomocí switch expression
        string type = arpEntry.dwType switch
        {
            MIB_IPNET_TYPE_DYNAMIC => "dynamic",  // Dynamicky naučený
            MIB_IPNET_TYPE_STATIC => "static",    // Staticky zadaný
            _ => "other"                          // Jiný typ
        };

        // Získání názvu síťového rozhraní
        string interfaceName = GetInterfaceName(arpEntry.dwIndex);

        // Výpis formátovaného záznamu
        Console.WriteLine($"{ipAddress,-15} {macAddress,-17} {type,-8} {interfaceName}");
    }

    static string GetInterfaceName(uint interfaceIndex)
    {
        try
        {
            // Získání všech síťových rozhraní
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Hledání rozhraní podle indexu
            foreach (NetworkInterface ni in interfaces)
            {
                var ipProperties = ni.GetIPProperties();
                if (ipProperties != null && ipProperties.GetIPv4Properties() != null)
                {
                    // Porovnání indexu rozhraní
                    if (ipProperties.GetIPv4Properties().Index == interfaceIndex)
                    {
                        return ni.Name;  // Nalezeno - vrátíme název
                    }
                }
            }
        }
        catch
        {
            // Při chybě vrátíme fallback hodnotu
        }

        // Fallback - vrátíme číslo rozhraní pokud název není nalezen
        return $"ifIndex:{interfaceIndex}";
    }
}

/*
Tento program čte ARP (Address Resolution Protocol) tabulku systému. Na Windows pomocí nativních Win32 API funkcí, na Linuxu ze souboru /proc/net/arp.
ARP tabulka mapuje IP adresy na fyzické MAC adresy v lokální síti.

Části programu:
Zjistí potřebnou velikost paměti pro ARP tabulku
Alokuje paměť a načte data
Prochází všechny záznamy
Pro každý záznam zobrazí:
IP adresu
MAC adresu
Typ záznamu (statický/dynamický)
Název síťového rozhraní

Chyby Win32 API se převádějí na Win32Exception a alokovaná paměť se uvolňuje v bloku finally.
*/