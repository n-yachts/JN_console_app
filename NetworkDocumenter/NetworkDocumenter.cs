using System;  // Import základních systémových funkcí a tříd
using System.Net;  // Import síťových funkcí (IP adresa, DNS atd.)
using System.Net.NetworkInformation;  // Import pro práci se síťovými rozhraními a statistikami
using System.Text;  // Import pro práci s textovými řetězci

class NetworkDocumenter  // Hlavní třída pro dokumentování sítě
{
    static void Main()  // Hlavní vstupní bod programu
    {
        // Vypsání záhlaví sestavy
        Console.WriteLine("=== NETWORK DOCUMENTATION REPORT ===\n");

        // Volání metod pro dokumentaci různých síťových komponent
        DocumentNetworkInterfaces();  // Dokumentace síťových rozhraní
        DocumentRoutingTable();  // Dokumentace směrovací tabulky
        DocumentDNSServers();  // Dokumentace DNS serverů
        DocumentNetworkStatistics();  // Dokumentace síťových statistik
    }

    static void DocumentNetworkInterfaces()  // Metoda pro dokumentaci síťových rozhraní
    {
        Console.WriteLine("1. SÍŤOVÁ ROZHRANÍ:");  // Nadpis sekce

        // Získání všech síťových rozhraní v systému
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        // Cyklus pro zpracování každého síťového rozhraní
        foreach (NetworkInterface ni in interfaces)
        {
            // Výpis základních informací o rozhraní
            Console.WriteLine($"\n{ni.Name} ({ni.NetworkInterfaceType})");  // Název a typ rozhraní
            Console.WriteLine($"  Status: {ni.OperationalStatus}");  // Aktuální stav (např. Up/Down)
            Console.WriteLine($"  Popis: {ni.Description}");  // Podrobný popis rozhraní
            Console.WriteLine($"  MAC: {ni.GetPhysicalAddress()}");  // Fyzická adresa (MAC)
            Console.WriteLine($"  Rychlost: {ni.Speed / 1000000} Mbps");  // Rychlost převodem na Mbps

            // Cyklus pro získání všech IP adres přiřazených k rozhraní
            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                // Filtrace pouze IPv4 adres
                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // Výpis IPv4 adresy a příslušné masky sítě
                    Console.WriteLine($"  IPv4: {ip.Address}/{ip.IPv4Mask}");
                }
            }
        }
        Console.WriteLine();  // Prázdný řádek pro lepší čitelnost
    }

    static void DocumentRoutingTable()  // Metoda pro dokumentaci směrovací tabulky
    {
        Console.WriteLine("2. SMĚROVACÍ TABULKA:");  // Nadpis sekce

        // Získání aktivních TCP spojení (použito pro kontext)
        var gateways = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();

        // Zjednodušené zobrazení výchozích bran
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        // Cyklus pro hledání bran u všech rozhraní
        foreach (NetworkInterface ni in interfaces)
        {
            // Získání seznamu bran pro konkrétní rozhraní
            var gateway = ni.GetIPProperties().GatewayAddresses;

            // Kontrola existence alespoň jedné brány
            if (gateway.Count > 0)
            {
                // Výpis první (výchozí) brány pro rozhraní
                Console.WriteLine($"  {ni.Name}: {gateway[0].Address}");
            }
        }
        Console.WriteLine();  // Prázdný řádek pro lepší čitelnost
    }

    static void DocumentDNSServers()  // Metoda pro dokumentaci DNS serverů
    {
        Console.WriteLine("3. DNS SERVERY:");  // Nadpis sekce

        // Získání všech síťových rozhraní
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        // Cyklus pro zpracování každého rozhraní
        foreach (NetworkInterface ni in interfaces)
        {
            // Získání DNS serverů konfigurovaných pro rozhraní
            var dnsServers = ni.GetIPProperties().DnsAddresses;

            // Kontrola existence DNS serverů
            if (dnsServers.Count > 0)
            {
                Console.WriteLine($"  {ni.Name}:");  // Výpis názvu rozhraní

                // Cyklus pro výpis všech DNS serverů
                foreach (IPAddress dns in dnsServers)
                {
                    Console.WriteLine($"    {dns}");  // Výpis IP adresy DNS serveru
                }
            }
        }
        Console.WriteLine();  // Prázdný řádek pro lepší čitelnost
    }

    static void DocumentNetworkStatistics()  // Metoda pro dokumentaci síťových statistik
    {
        Console.WriteLine("4. STATISTIKY:");  // Nadpis sekce

        // Získání globálních síťových vlastností
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

        // Výpis počtu aktivních TCP spojení
        Console.WriteLine($"  Aktivní TCP spojení: {properties.GetActiveTcpConnections().Length}");

        // Výpis počtu TCP portů v režimu naslouchání
        Console.WriteLine($"  Aktivní TCP listenery: {properties.GetActiveTcpListeners().Length}");

        // Výpis počtu UDP portů v režimu naslouchání
        Console.WriteLine($"  Aktivní UDP listenery: {properties.GetActiveUdpListeners().Length}");
    }
}

/*
Using direktivy - Načítají potřebné jmenné prostory pro práci se sítí
Hlavní třída - Organizuje funkce pro dokumentování sítě
Metoda Main - Koordinuje volání jednotlivých dokumentačních metod
DocumentNetworkInterfaces - Zobrazuje detailní informace o všech síťových adaptérech
DocumentRoutingTable - Ukazuje výchozí brány pro jednotlivá rozhraní
DocumentDNSServers - Vypisuje DNS servery konfigurované pro každé rozhraní
DocumentNetworkStatistics - Poskytuje přehled o síťových připojeních

Program generuje ucelený přehled o síťové konfiguraci systému, včetně IP adres, MAC adres, bran, DNS a síťových statistik.
*/