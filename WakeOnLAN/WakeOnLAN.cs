using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class WakeOnLAN
{
    static void Main(string[] args)
    {
        // Kontrola počtu argumentů - program vyžaduje přesně jeden argument (MAC adresu)
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: WakeOnLAN <MAC_address>");
            Console.WriteLine("Příklad: WakeOnLAN 00-11-22-33-44-55");
            return; // Ukončení programu při chybném počtu argumentů
        }

        // Odstranění oddělovačů z MAC adresy (nahrazení dvojteček a pomlček prázdným znakem)
        string macAddress = args[0].Replace(":", "").Replace("-", "");

        // Kontrola délky MAC adresy po odstranění oddělovačů (musí být 12 znaků)
        if (macAddress.Length != 12)
        {
            Console.WriteLine("Neplatná MAC adresa");
            return; // Ukončení programu při neplatné délce MAC adresy
        }

        // Vytvoření WoL packetu o velikosti 102 bytů
        byte[] packet = new byte[102];

        // Prvních 6 bytů se vyplní hodnotou 0xFF (magický paket začíná 6x 0xFF)
        for (int i = 0; i < 6; i++)
            packet[i] = 0xFF;

        // Následuje 16x opakování cílové MAC adresy (16 * 6 = 96 bytů)
        for (int i = 1; i <= 16; i++)
        {
            // Zpracování každého bytu MAC adresy (6 bytů)
            for (int j = 0; j < 6; j++)
            {
                // Převod dvou znaků HEX stringu na byte a vložení do packetu
                packet[i * 6 + j] = Convert.ToByte(macAddress.Substring(j * 2, 2), 16);
            }
        }

        // Vytvoření UDP klienta pro odeslání packetu
        using UdpClient client = new UdpClient();
        // Odeslání broadcast packetu na UDP port 9 (standardní port pro Wake-on-LAN)
        client.Send(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));

        // Potvrzení o odeslání packetu
        Console.WriteLine($"WoL packet odeslán pro MAC: {args[0]}");
    }
}

/*
Kontrola vstupních argumentů
 Program vyžaduje jako vstupní parametr MAC adresu zařízení, které se má probudit.
Normalizace MAC adresy
 Odstraní se zadané oddělovače (- nebo :) aby zůstal pouze 12místný HEX řetězec.
Validace délky MAC adresy
 Po odstranění oddělovačů musí MAC adresa mít přesně 12 znaků.
Tvorba magického paketu
 Prvních 6 bytů: 0xFF (signatura WoL paketu)
 Následuje 16× opakovaná MAC adresa (96 bytů)
 Celková velikost: 102 bytů
Odeslání přes UDP
 Paket se odešle jako broadcast na UDP port 9 (standardní port pro WoL).
Vyčištění zdrojů
 Použití using zajišťuje automatické uvolnění UDP klienta.
*/