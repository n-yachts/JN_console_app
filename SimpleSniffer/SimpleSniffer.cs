using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class SimpleSniffer
{
    static void Main()
    {
        // Výpis úvodní informace o programu
        Console.WriteLine("Základní síťový sniffer - zachytává ICMP a TCP pakety\n");

        // Vytvoření raw socketu pro zachytávání síťových paketů
        // AddressFamily.InterNetwork = IPv4 adresy
        // SocketType.Raw = Raw socket umožňující čtení celých paketů včetně hlaviček
        // ProtocolType.IP = Zachycení paketů na IP úrovni
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

        // Navázání socketu na konkrétní IP adresu a port
        // IPAddress.Parse("127.0.0.1") = Naslouchání na localhostu
        // Port 0 = systém automaticky přiřadí volný port
        socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));

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
Funguje pouze pro localhost (127.0.0.1) - pro zachycování veškerého provozu je třeba změnit na IPAddress.Any

Tento kód demonstruje základní princip síťového sniffování, ale v reálném nasazení by bylo vhodné doplnit:
Ošetření výjimek
Podrobnější analýzu paketů
Možnost filtrování
Ukládání do souboru
Vícevláknové zpracování
*/