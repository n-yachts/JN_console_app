using System;  // Základní jmenný prostor pro Console a BitConverter
using System.Net;  // IPAddress, IPEndPoint, Dns
using System.Net.Sockets;  // Socket, IOControlCode, nastavení raw socketu

class SimpleSniffer  // Jednoduchý sniffer IP paketů (raw socket)
{
    static void Main()
    {
        // Výpis úvodní informace o programu
        Console.WriteLine("Základní síťový sniffer - zachytává IP pakety (ICMP, TCP, UDP...)\n");

        // Zjištění IPv4 adresy lokálního rozhraní, na kterém se bude zachytávat
        // (bind na 127.0.0.1 by zachytil jen loopback provoz)
        IPAddress localIp = null;
        foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
            {
                localIp = address;
                break;
            }
        }
        if (localIp == null)
        {
            Console.WriteLine("Nebyla nalezena žádná IPv4 adresa lokálního rozhraní.");
            return;
        }

        // Vytvoření raw socketu pro zachytávání síťových paketů
        // AddressFamily.InterNetwork = IPv4 adresy
        // SocketType.Raw = Raw socket umožňující čtení celých paketů včetně hlaviček
        // ProtocolType.IP = Zachycení paketů na IP úrovni
        Socket socket;
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            socket.Bind(new IPEndPoint(localIp, 0));
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Chyba: {ex.Message} (spusťte program jako správce)");
            return;
        }
        Console.WriteLine($"Zachytávám na rozhraní {localIp}\n");

        // Nastavení socketové option pro includování IP hlavičky v přijatých datech
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

        // Příprava hodnot pro IOControl operaci
        // BitConverter.GetBytes(1) = Povolení promiskuitního režimu (1 = true)
        byte[] inValue = BitConverter.GetBytes(1);
        // BitConverter.GetBytes(0) = Výstupní parametr (není využit)
        byte[] outValue = BitConverter.GetBytes(0);

        // Aktivace IOControl pro příjem všech paketů (promiskuitní režim)
        // IOControlCode.ReceiveAll = Zachytávání všech paketů včetně těch nesměrovaných k nám
        socket.IOControl(IOControlCode.ReceiveAll, inValue, outValue);

        // Buffer pro ukládání přijatých paketů (velikost 4KB)
        byte[] buffer = new byte[4096];

        // Hlavní smyčka pro průběžné zachytávání paketů
        while (true)
        {
            // Čtení dat z socketu do bufferu
            int bytesRead = socket.Receive(buffer);

            // Zpracování pouze neprázdných paketů
            if (bytesRead > 0)
            {
                // Extrakce zdrojové IP adresy z IP hlavičky
                // Pozice 12-15 v IP hlavičce obsahuje zdrojovou IP adresu
                IPAddress sourceIP = new IPAddress(BitConverter.ToUInt32(buffer, 12));

                // Extrakce cílové IP adresy z IP hlavičky
                // Pozice 16-19 v IP hlavičce obsahuje cílovou IP adresu
                IPAddress destIP = new IPAddress(BitConverter.ToUInt32(buffer, 16));

                // Extrakce protokolu z IP hlavičky (pozice 9)
                // 1 = ICMP, 6 = TCP, 17 = UDP, atd.
                byte protocol = buffer[9];

                // Výpis informací o zachyceném paketu
                Console.WriteLine($"Paket: {sourceIP} -> {destIP} Protocol: {protocol} Velikost: {bytesRead} bytes");
            }
        }
    }
}

/*
Kód funguje pouze na Windows (kvůli IOControlCode.ReceiveAll)
Vyžaduje spuštění s administrátorskými právy
Zachycuje pouze IPv4 komunikaci
Zobrazuje základní informace z IP hlavičky, neanalyzuje transportní vrstvu
Zachytává provoz na prvním nalezeném IPv4 rozhraní počítače (ne jen na loopbacku); na Wi-Fi adaptérech nemusí promiskuitní režim fungovat

Tento kód demonstruje základní princip síťového sniffování, ale v reálném nasazení by bylo vhodné doplnit:
Rozsáhlejší ošetření výjimek (ošetřeno je jen vytvoření socketu)
Podrobnější analýzu paketů
Možnost filtrování
Ukládání do souboru
Vícevláknové zpracování
*/