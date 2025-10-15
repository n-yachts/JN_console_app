using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class WakeOnLAN
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: WakeOnLAN <MAC_address>");
            Console.WriteLine("Příklad: WakeOnLAN 00-11-22-33-44-55");
            return;
        }

        string macAddress = args[0].Replace(":", "").Replace("-", "");

        if (macAddress.Length != 12)
        {
            Console.WriteLine("Neplatná MAC adresa");
            return;
        }

        // WoL packet: 6x 0xFF + 16x MAC address
        byte[] packet = new byte[102];
        for (int i = 0; i < 6; i++)
            packet[i] = 0xFF;

        for (int i = 1; i <= 16; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                packet[i * 6 + j] = Convert.ToByte(macAddress.Substring(j * 2, 2), 16);
            }
        }

        using UdpClient client = new UdpClient();
        client.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));

        Console.WriteLine($"WoL packet odeslán pro MAC: {args[0]}");
    }
}