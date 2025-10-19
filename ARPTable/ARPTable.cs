using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ComponentModel;

class ARPTable
{
    // Import Win32 API funkce pro získání ARP tabulky
    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetIpNetTable(IntPtr pIpNetTable, ref uint pdwSize, bool bOrder);

    // Import Win32 API funkce pro uvolnění paměti
    [DllImport("iphlpapi.dll")]
    static extern uint FreeMibTable(IntPtr plpNetTable);

    // Konstanty pro typy záznamů v ARP tabulce
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
        public uint dwAddr;         // IP adresa (ve formátu uint)
        public uint dwType;         // Typ záznamu (dynamic/static)
    }

    // Hlavní struktura ARP tabulky
    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETTABLE
    {
        public uint dwNumEntries;   // Počet záznamů v tabulce
        public MIB_IPNETROW table;  // První záznam (ostatní následují v paměti za ním)
    }

    static void Main()
    {
        // Výpis hlavičky programu
        Console.WriteLine("ARP Table - Lokální cache (Win32 API)\n");

        // Formátování sloupců výpisu
        Console.WriteLine("{0,-15} {1,-17} {2,-8} {3}", "IP Address", "Physical Address", "Type", "Interface");
        Console.WriteLine(new string('-', 60));  // Oddělovací čára

        try
        {
            // Zavolání hlavní metody pro zobrazení ARP tabulky
            DisplayARPTable();
        }
        catch (Exception ex)
        {
            // Zachycení a výpis případných chyb
            Console.WriteLine($"Chyba: {ex.Message}");
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
Tento program čte ARP (Address Resolution Protocol) tabulku systému Windows pomocí nativních Win32 API funkcí.
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

Program obsahuje kompletní ošetření chyb a korektní práci s pamětí.
*/