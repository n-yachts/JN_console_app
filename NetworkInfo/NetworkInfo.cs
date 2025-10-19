using System;  // Importuje základní systémové knihovny pro práci s konzolí, výjimkami atd.
using System.Net;  // Importuje knihovny pro práci se síťovými operacemi (DNS, IP adresy)
using System.Net.NetworkInformation;  // Importuje knihovny pro získávání síťových informací

class NetworkInfo  // Definice třídy obsahující síťové informace
{
    static void Main()  // Hlavní vstupní bod programu
    {
        // Získání a výpis názvu lokálního počítače
        string hostName = Dns.GetHostName();  // Volá metodu GetHostName() ze třídy Dns pro získání názvu stroje
        Console.WriteLine($"Hostname: {hostName}");  // Výpis hostname do konzole pomocí interpolace řetězce

        // Získání seznamu všech síťových rozhraní
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();  // Získá všechna síťová rozhraní systému

        // Cyklus procházející všechna síťová rozhraní
        foreach (NetworkInterface ni in interfaces)  // Iteruje přes každé síťové rozhraní v poli
        {
            // Kontrola, zda je rozhraní aktivní
            if (ni.OperationalStatus == OperationalStatus.Up)  // Podmínka kontroluje, zda je rozhraní v provozním stavu "Up"
            {
                // Výpis základních informací o rozhraní
                Console.WriteLine($"\nRozhraní: {ni.Name}");  // Vypíše název rozhraní
                Console.WriteLine($"  Typ: {ni.NetworkInterfaceType}");  // Vypíše typ rozhraní (Ethernet, WiFi, atd.)
                Console.WriteLine($"  Popis: {ni.Description}");  // Vypíše popis rozhraní

                // Získání všech IP adres přiřazených k rozhraní
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)  // Cyklus prochází všechny unicast IP adresy
                {
                    // Filtrace IPv4 adres
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)  // Kontrola, zda se jedná o IPv4 adresu
                    {
                        Console.WriteLine($"  IPv4: {ip.Address}");  // Výpis IPv4 adresy
                        Console.WriteLine($"  Maska: {ip.IPv4Mask}");  // Výpis příslušné masky sítě
                    }
                }
            }
        }
    }
}

/*
Získání hostname:
 Metoda Dns.GetHostName() vrátí název lokálního počítače v síti
Práce se síťovými rozhraními:
 GetAllNetworkInterfaces() načte všechna dostupná síťová rozhraní
 Cyklus foreach prochází jednotlivá rozhraní
 Podmínka kontroluje, zda je rozhraní aktivní (OperationalStatus.Up)
Výpis informací:
 Pro každé aktivní rozhraní se vypíše:
  Název
  Typ (Ethernet, Wireless, Loopback...)
  Technický popis
 Dále se prochází všechny IP adresy rozhraní
Filtrace IPv4:
 AddressFamily.InterNetwork zajistí, že se zobrazí pouze IPv4 adresy
Pro každou IPv4 adresu se vypíše:
 Samotná IP adresa
 Maska podsítě

Tento program tak poskytuje přehlednou konzolovou informaci o všech aktivních síťových rozhraních a jejich IPv4 konfiguraci na lokálním počítači.
*/