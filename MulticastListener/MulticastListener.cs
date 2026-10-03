using System;                   // Základní jmenný prostor pro vstup/výstup, řetězce atd.
using System.Net;              // Třídy pro práci se sítí (IPAddress, IPEndPoint)
using System.Net.NetworkInformation;  // Výběr síťových rozhraní
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
        // Povolení sdílení portu s dalšími posluchači (musí být před Bind)
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        // Propojení socketu se všemi dostupnými rozhraními na zadaném portu (musí být před připojením ke skupině)
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        // Připojení ke specifické multicast skupině přes lokální IPv4 rozhraní.
        // Bez zadání rozhraní si systém vybírá podle routovací tabulky, což může selhat (např. VPN, chybějící výchozí trasa).
        bool joined = false;
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up || !ni.SupportsMulticast ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                try
                {
                    client.JoinMulticastGroup(multicastGroup, ip.Address);
                    Console.WriteLine($"Připojeno ke skupině přes rozhraní {ni.Name} ({ip.Address})");
                    joined = true;
                }
                catch (SocketException)
                {
                    // Toto rozhraní multicast nepodporuje, zkusí se další
                }
            }
        }

        if (!joined)
        {
            Console.WriteLine("Nepodařilo se připojit ke skupině na žádném síťovém rozhraní.");
            return;
        }

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