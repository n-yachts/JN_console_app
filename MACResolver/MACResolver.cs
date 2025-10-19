using System;  // Import základních systémových knihoven
using System.Net;  // Import knihoven pro práci se sítí (DNS, IP adresy)
using System.Net.NetworkInformation;  // Import pro práci se síťovými rozhraními (MAC adresy)

class MACResolver  // Hlavní třída programu
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje přesně jeden argument
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: MACResolver <IP/hostname>");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        string target = args[0];  // Uložení zadaného argumentu (IP/hostname)

        try  // Ošetření možných chyb při práci se sítí
        {
            // Překlad hostname na IP adresu (pro IP adresu vrací přímo danou adresu)
            IPAddress[] addresses = Dns.GetHostAddresses(target);

            // Kontrola úspěšnosti překladu
            if (addresses.Length == 0)
            {
                Console.WriteLine("Nenalezena IP adresa pro zadaný hostname");
                return;
            }

            // Procházení všech vrácených IP adres
            foreach (IPAddress address in addresses)
            {
                // Filtrace pouze na IPv4 adresy (InterNetwork = IPv4)
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // Volání metody pro získání MAC adresy
                    PhysicalAddress mac = GetMACAddress(address);

                    // Zpracování pouze platných výsledků
                    if (mac != null)
                    {
                        Console.WriteLine($"IP: {address}");
                        Console.WriteLine($"MAC: {FormatMAC(mac)}");  // Formátování MAC adresy
                    }
                }
            }
        }
        catch (Exception ex)  // Zachycení všech možných chyb
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static PhysicalAddress GetMACAddress(IPAddress ip)
    {
        // Metoda pro hledání MAC adresy k místní IP adrese
        // POZOR: Funguje pouze pro lokální síťová rozhraní!

        // Procházení všech síťových rozhraní
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Procházení všech IP adres každého rozhraní
            foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses)
            {
                // Hledání shody IP adresy
                if (ipInfo.Address.Equals(ip))
                {
                    // Vrácení příslušné MAC adresy
                    return ni.GetPhysicalAddress();
                }
            }
        }
        return null;  // Nenalezena odpovídající MAC adresa
    }

    static string FormatMAC(PhysicalAddress mac)
    {
        // Metoda pro formátování MAC adresy z řetězce čísel na standardní formát s dvojtečkami

        string macString = mac.ToString();  // Původní formát např.: "001122AABBCC"

        // Rozděnění řetězce po dvou znacích a spojení dvojtečkami
        return string.Join(":",
            macString.Substring(0, 2),
            macString.Substring(2, 2),
            macString.Substring(4, 2),
            macString.Substring(6, 2),
            macString.Substring(8, 2),
            macString.Substring(10, 2));
    }
}

/*
Tento kód funguje pouze pro lokální síťová rozhraní (IP adresy přiřazené vašemu počítači)
Pro získání MAC adresy vzdálených zařízení by bylo nutné použít ARP protokol nebo jiné síťové nástroje
Většina moderních sítí a firewallů blokuje přímé dotazy na MAC adresy vzdálených zařízení
*/