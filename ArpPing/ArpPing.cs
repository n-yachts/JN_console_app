using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

class ArpPing
{
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    static extern int SendARP(int destIp, int srcIp, byte[] macAddr, ref uint physicalAddrLen);

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: ArpPing <IP adresa>");
            return;
        }

        IPAddress target = IPAddress.Parse(args[0]);
        byte[] macAddr = new byte[6];
        uint macAddrLen = (uint)macAddr.Length;

        int result = SendARP((int)target.Address, 0, macAddr, ref macAddrLen);
        if (result == 0)
        {
            string[] str = new string[macAddrLen];
            for (int i = 0; i < macAddrLen; i++)
                str[i] = macAddr[i].ToString("X2");
            Console.WriteLine($"MAC adresa: {string.Join(":", str)}");
        }
        else
        {
            Console.WriteLine("Chyba: Nelze získat MAC adresu");
        }
    }
}