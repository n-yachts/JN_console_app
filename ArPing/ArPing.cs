using System;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

namespace ArPing
{
    internal class ArPing
    {
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(
            int destIp,
            int srcIp,
            byte[] macAddr,
            ref uint physicalAddrLen);

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Použití: ArPing <IP adresa>");
                return;
            }

            try
            {
                IPAddress target = IPAddress.Parse(args[0]);
                byte[] macAddr = new byte[6];
                uint macLen = (uint)macAddr.Length;

                int result = SendARP(
                    BitConverter.ToInt32(target.GetAddressBytes(), 0),
                    0,
                    macAddr,
                    ref macLen);

                if (result == 0 && macLen > 0)
                {
                    Console.WriteLine($"IP: {target} -> MAC: {FormatMacAddress(macAddr)}");
                }
                else
                {
                    Console.WriteLine($"Zařízení {target} nebylo nalezeno v ARP tabulce");
                    if (result != 0)
                    {
                        Console.WriteLine($"Chyba ARP: {new Win32Exception(result).Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }

        private static string FormatMacAddress(byte[] mac)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < mac.Length; i++)
            {
                sb.Append(mac[i].ToString("X2"));
                if (i < mac.Length - 1) sb.Append("-");
            }
            return sb.ToString();
        }
    }
}