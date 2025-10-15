using System;
using System.Net;

class SubnetCalculator
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Použití: SubnetCalculator <IP/maska> <počet hostů>");
            Console.WriteLine("Příklad: SubnetCalculator 192.168.1.0/24 50");
            return;
        }

        string[] ipParts = args[0].Split('/');
        IPAddress ip = IPAddress.Parse(ipParts[0]);
        int maskBits = int.Parse(ipParts[1]);
        int requiredHosts = int.Parse(args[1]);

        // Výpočet nové masky
        int newMaskBits = 32 - (int)Math.Ceiling(Math.Log(requiredHosts + 2, 2));
        uint mask = 0xFFFFFFFFu << (32 - newMaskBits);

        Console.WriteLine($"Původní síť: {ip}/{maskBits}");
        Console.WriteLine($"Požadovaných hostů: {requiredHosts}");
        Console.WriteLine($"Nová maska: /{newMaskBits}");
        Console.WriteLine($"Skutečná kapacita: {Math.Pow(2, 32 - newMaskBits) - 2} hostů");
    }
}