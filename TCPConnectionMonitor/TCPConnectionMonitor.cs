using System;                      // Import základních systémových funkcí a tříd
using System.Net;                  // Import tříd pro práci se sítí (IP adresy, koncové body)
using System.Net.NetworkInformation; // Import tříd pro monitorování síťových připojení

class TCPConnectionMonitor         // Hlavní třída aplikace
{
    static void Main()             // Hlavní vstupní bod aplikace
    {
        // Výpis úvodní zprávy
        Console.WriteLine("Aktivní TCP spojení:\n");

        // Získání síťových informací o aktuálním zařízení
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

        // Načtení všech aktivních TCP připojení
        TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

        // Cyklus pro zpracování každého nalezeného připojení
        foreach (TcpConnectionInformation c in connections)
        {
            // Výpis informací o připojení:
            // - Lokální koncový bod (IP adresa a port)
            // - Vzdálený koncový bod (IP adresa a port)
            // - Aktuální stav spojení (Established, TimeWait, atd.)
            Console.WriteLine($"Místní: {c.LocalEndPoint} -> Vzdálené: {c.RemoteEndPoint} | Stav: {c.State}");
        }
    }
}

/*
Using direktivy - Načtení potřebných jmenných prostorů pro práci se sítí
Třída TCPConnectionMonitor - Hlavní kontejner pro aplikaci
Metoda Main() - Vstupní bod programu
IPGlobalProperties - Třída poskytující globální síťové informace
GetActiveTcpConnections() - Získá všechny aktivní TCP spojení
Cyklus foreach - Iteruje přes každé nalezené spojení
TcpConnectionInformation - Obsahuje:
 LocalEndPoint: Lokální IP a port
 RemoteEndPoint: Vzdálená IP a port
 State: Stav spojení (např. Established, Closed, Listen)
*/