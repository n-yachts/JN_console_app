using System;
using System.Linq;
using System.Net;

class IPCalculator
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: IPCalculator <IP/maska>");
            Console.WriteLine("Příklad: IPCalculator 192.168.1.0/24");
            return;
        }

        string[] parts = args[0].Split('/');
        if (parts.Length != 2)
        {
            Console.WriteLine("Neplatný formát. Použijte formát IP/maska.");
            return;
        }

        IPAddress ipAddress = IPAddress.Parse(parts[0]);
        int maskLength = int.Parse(parts[1]);

        // Výpočet masky sítě
        uint mask = 0xFFFFFFFFu << (32 - maskLength);
        IPAddress subnetMask = new IPAddress(BitConverter.GetBytes(mask).Reverse().ToArray());

        // Výpočet adresy sítě
        byte[] ipBytes = ipAddress.GetAddressBytes();
        byte[] maskBytes = subnetMask.GetAddressBytes();
        byte[] networkBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
        }
        IPAddress networkAddress = new IPAddress(networkBytes);

        // Výpočet broadcast adresy
        byte[] broadcastBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            broadcastBytes[i] = (byte)(networkBytes[i] | ~maskBytes[i]);
        }
        IPAddress broadcastAddress = new IPAddress(broadcastBytes);

        // Výpočet první a poslední použitelné adresy
        byte[] firstUsableBytes = networkBytes;
        firstUsableBytes[3] += 1;
        IPAddress firstUsable = new IPAddress(firstUsableBytes);

        byte[] lastUsableBytes = broadcastBytes;
        lastUsableBytes[3] -= 1;
        IPAddress lastUsable = new IPAddress(lastUsableBytes);

        Console.WriteLine($"IP adresa: {ipAddress}");
        Console.WriteLine($"Maska sítě: {subnetMask} (/{maskLength})");
        Console.WriteLine($"Adresa sítě: {networkAddress}");
        Console.WriteLine($"Broadcast: {broadcastAddress}");
        Console.WriteLine($"Rozsah hostů: {firstUsable} - {lastUsable}");
        Console.WriteLine($"Počet hostů: {Math.Pow(2, 32 - maskLength) - 2}");
    }
}