using System;                   // Základní jmenný prostor pro vstup/výstup, řetězce atd.
using System.Net;              // Třídy pro práci se sítí (IPAddress, IPEndPoint)
using System.Net.Sockets;      // Třídy pro síťovou komunikaci (UdpClient)
using System.Text;             // Práce s kódováním textu (Encoding.UTF8)

class MulticastListener        // Hlavní třída aplikace
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů
        if (args.Length != 2)
        {
            // Výpis správného použití při chybném počtu argumentů
            Console.WriteLine("Použití: MulticastListener <multicast_group> <port>");
            Console.WriteLine("Příklad: MulticastListener 224.0.0.1 5000");
            return;  // Předčasné ukončení programu
        }

        // Parsování prvního argumentu na IP adresu multicast skupiny
        IPAddress multicastGroup = IPAddress.Parse(args[0]);
        // Parsování druhého argumentu na číslo portu
        int port = int.Parse(args[1]);

        // Vytvoření nového UDP klienta pro síťovou komunikaci
        UdpClient client = new UdpClient();
        // Připojení ke specifické multicast skupině
        client.JoinMulticastGroup(multicastGroup);
        // Propojení socketu se všemi dostupnými rozhraními na zadaném portu
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        // Informace o úspěšném spuštění naslouchání
        Console.WriteLine($"Naslouchám multicast skupině {multicastGroup}:{port}");

        // Příprava objektu pro ukládání informací o odesílateli
        // IPAddress.Any = přijímáme z jakékoli IP adresy
        // port 0 = jakýkoli port (bude přepsán při přijetí zprávy)
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        // Nekonečná smyčka pro průběžné přijímání zpráv
        while (true)
        {
            // Přijetí datagramu a uložení informací o odesílateli
            byte[] data = client.Receive(ref remote);
            // Převod binárních dat na textový řetězec pomocí UTF-8 kódování
            string message = Encoding.UTF8.GetString(data);
            // Výpis zprávy včetně informací o odesílateli
            Console.WriteLine($"[{remote}] {message}");
        }
    }
}

/*
Multicast komunikace:
 Skupinová komunikace (jeden → mnoho)
 Adresy v rozsahu 224.0.0.0 až 239.255.255.255
 Efektivní pro streamování nebo hromadné oznamování
Důležité vlastnosti:
 IPAddress.Any - naslouchá na všech síťových rozhraních
 UdpClient.Receive() - blokující operace (čeká na data)
 Používá UDP protokol (bez spojení, nedoručené zprávy se neopakují)
Typické použití:
 Síťové služby (vyhledávání zařízení)
 Přenos video/audio streamů
 Distribuce dat v reálném čase
*/