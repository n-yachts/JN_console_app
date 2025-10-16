/*
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

class ARPTable
{
    static void Main()
    {
        Console.WriteLine("ARP Table - Lokální cache\n");

        try
        {
            var arpProcess = new Process();
            arpProcess.StartInfo.FileName = "arp";
            arpProcess.StartInfo.Arguments = "-a";
            arpProcess.StartInfo.UseShellExecute = false;
            arpProcess.StartInfo.RedirectStandardOutput = true;
            arpProcess.StartInfo.CreateNoWindow = true;

            arpProcess.Start();
            string output = arpProcess.StandardOutput.ReadToEnd();
            arpProcess.WaitForExit();

            Console.WriteLine(output);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}
*/
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ComponentModel;

class ARPTable
{
    // Win32 API struktury a funkce
    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetIpNetTable(IntPtr pIpNetTable, ref uint pdwSize, bool bOrder);

    [DllImport("iphlpapi.dll")]
    static extern uint FreeMibTable(IntPtr plpNetTable);

    const int MIB_IPNET_TYPE_DYNAMIC = 3;
    const int MIB_IPNET_TYPE_STATIC = 4;

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETROW
    {
        public uint dwIndex;
        public uint dwPhysAddrLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] bPhysAddr;
        public uint dwAddr;
        public uint dwType;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETTABLE
    {
        public uint dwNumEntries;
        public MIB_IPNETROW table;
    }

    static void Main()
    {
        Console.WriteLine("ARP Table - Lokální cache (Win32 API)\n");
        Console.WriteLine("{0,-15} {1,-17} {2,-8} {3}", "IP Address", "Physical Address", "Type", "Interface");
        Console.WriteLine(new string('-', 60));

        try
        {
            DisplayARPTable();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static void DisplayARPTable()
    {
        uint bufferSize = 0;
        uint result = GetIpNetTable(IntPtr.Zero, ref bufferSize, false);

        if (result != 122) // ERROR_INSUFFICIENT_BUFFER
        {
            throw new Win32Exception((int)result);
        }

        IntPtr buffer = Marshal.AllocCoTaskMem((int)bufferSize);

        try
        {
            result = GetIpNetTable(buffer, ref bufferSize, false);
            if (result != 0)
            {
                throw new Win32Exception((int)result);
            }

            // Přesun na začátek tabulky
            IntPtr currentEntry = buffer;
            uint entriesCount = (uint)Marshal.ReadInt32(currentEntry);
            currentEntry += 4; // Přeskočit dwNumEntries

            for (int i = 0; i < entriesCount; i++)
            {
                MIB_IPNETROW arpEntry = (MIB_IPNETROW)Marshal.PtrToStructure(
                    currentEntry, typeof(MIB_IPNETROW));

                DisplayARPEntry(arpEntry);
                currentEntry += Marshal.SizeOf(typeof(MIB_IPNETROW));
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(buffer);
        }
    }

    static void DisplayARPEntry(MIB_IPNETROW arpEntry)
    {
        // Převod IP adresy
        IPAddress ipAddress = new IPAddress(arpEntry.dwAddr);

        // Převod MAC adresy
        string macAddress = BitConverter.ToString(arpEntry.bPhysAddr, 0, (int)arpEntry.dwPhysAddrLen);

        // Typ záznamu
        string type = arpEntry.dwType switch
        {
            MIB_IPNET_TYPE_DYNAMIC => "dynamic",
            MIB_IPNET_TYPE_STATIC => "static",
            _ => "other"
        };

        // Název rozhraní
        string interfaceName = GetInterfaceName(arpEntry.dwIndex);

        Console.WriteLine($"{ipAddress,-15} {macAddress,-17} {type,-8} {interfaceName}");
    }

    static string GetInterfaceName(uint interfaceIndex)
    {
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                var ipProperties = ni.GetIPProperties();
                if (ipProperties != null && ipProperties.GetIPv4Properties() != null)
                {
                    if (ipProperties.GetIPv4Properties().Index == interfaceIndex)
                    {
                        return ni.Name;
                    }
                }
            }
        }
        catch
        {
            // Pokud se nepodaří najít název, vrátíme index
        }
        return $"ifIndex:{interfaceIndex}";
    }
}