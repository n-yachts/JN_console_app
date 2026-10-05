using System;  // Základní jmenný prostor pro Console a BitConverter
using System.Net;  // IPAddress, IPEndPoint, Dns
using System.Net.Sockets;  // Socket, IOControlCode, nastavení raw socketu

class SimpleSniffer  // Jednoduchý sniffer IP paketů (raw socket; Windows a Linux)
{
    static void Main()
    {
        // Výpis úvodní informace o programu
        Console.WriteLine("Základní síťový sniffer - zachytává IP pakety (ICMP, TCP, UDP...)\n");

        bool isLinux = OperatingSystem.IsLinux();
        if (!isLinux && !OperatingSystem.IsWindows())
        {
            Console.WriteLine("Tento systém není podporován (jen Windows a Linux).");
            return;
        }

        Socket socket;
        try
        {
            if (isLinux)
            {
                // Linux: packet socket (AF_PACKET) s protokolem ETH_P_ALL (0x0003 v síťovém pořadí bajtů) zachytává
                // všechny rámce na všech rozhraních včetně Ethernet hlavičky; vyžaduje root (nebo CAP_NET_RAW)
                socket = new Socket(AddressFamily.Packet, SocketType.Raw, (ProtocolType)IPAddress.HostToNetworkOrder((short)0x0003));
                Console.WriteLine("Zachytávám na všech rozhraních\n");
            }
            else
            {
                // Windows: zjištění IPv4 adresy lokálního rozhraní, na kterém se bude zachytávat
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
                // SocketType.Raw = Raw socket umožňující čtení celých paketů včetně hlaviček
                // ProtocolType.IP = Zachycení paketů na IP úrovni
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                socket.Bind(new IPEndPoint(localIp, 0));
                Console.WriteLine($"Zachytávám na rozhraní {localIp}\n");

                // Nastavení socketové option pro includování IP hlavičky v přijatých datech
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

                // BitConverter.GetBytes(1) = Povolení promiskuitního režimu (1 = true)
                byte[] inValue = BitConverter.GetBytes(1);
                byte[] outValue = BitConverter.GetBytes(0);

                // IOControlCode.ReceiveAll = Zachytávání všech paketů včetně těch nesměrovaných k nám (jen Windows)
                socket.IOControl(IOControlCode.ReceiveAll, inValue, outValue);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Chyba: {ex.Message} ({(isLinux ? "spusťte program jako root, např. sudo ./SimpleSniffer" : "spusťte program jako správce")})");
            return;
        }

        // Buffer pro ukládání přijatých paketů (velikost 4KB)
        byte[] buffer = new byte[4096];

        // Hlavní smyčka pro průběžné zachytávání paketů
        while (true)
        {
            // Čtení dat z socketu do bufferu
            int bytesRead = socket.Receive(buffer);

            // Posun začátku IP hlavičky v bufferu: na Windows 0, na Linuxu za Ethernet hlavičkou
            int ip = 0;

            if (isLinux)
            {
                if (bytesRead < 14) continue;

                // Ethernet hlavička: 6 B cílová MAC, 6 B zdrojová MAC, 2 B typ (0x0800 = IPv4)
                int etherType = (buffer[12] << 8) | buffer[13];
                ip = 14;

                // Rámec s VLAN tagem (802.1Q) má před typem ještě 4 bajty
                if (etherType == 0x8100 && bytesRead >= 18)
                {
                    etherType = (buffer[16] << 8) | buffer[17];
                    ip = 18;
                }

                if (etherType != 0x0800) continue;  // Zajímá nás jen IPv4 (ARP, IPv6 a další se přeskočí)
            }

            // Zpracování pouze paketů, které obsahují celou IP hlavičku
            if (bytesRead - ip >= 20)
            {
                // Extrakce zdrojové IP adresy z IP hlavičky
                // Pozice 12-15 v IP hlavičce obsahuje zdrojovou IP adresu
                IPAddress sourceIP = new IPAddress(BitConverter.ToUInt32(buffer, ip + 12));

                // Extrakce cílové IP adresy z IP hlavičky
                // Pozice 16-19 v IP hlavičce obsahuje cílovou IP adresu
                IPAddress destIP = new IPAddress(BitConverter.ToUInt32(buffer, ip + 16));

                // Extrakce protokolu z IP hlavičky (pozice 9)
                // 1 = ICMP, 6 = TCP, 17 = UDP, atd.
                byte protocol = buffer[ip + 9];

                // Výpis informací o zachyceném paketu
                Console.WriteLine($"Paket: {sourceIP} -> {destIP} Protocol: {protocol} Velikost: {bytesRead} bytes");
            }
        }
    }
}

/*
Windows: raw socket navázaný na adresu rozhraní, promiskuitní režim přes IOControlCode.ReceiveAll (jen Windows)
Linux: packet socket (AF_PACKET, ETH_P_ALL) - zachytává všechny rámce na všech rozhraních; IP hlavička začíná za Ethernet hlavičkou (14 bajtů, s VLAN tagem 18)
Vyžaduje administrátorská práva (Windows) nebo root / CAP_NET_RAW (Linux)
Zachycuje pouze IPv4 komunikaci
Zobrazuje základní informace z IP hlavičky, neanalyzuje transportní vrstvu
Na Windows se zachytává na prvním nalezeném IPv4 rozhraní (ne jen na loopbacku); na Wi-Fi adaptérech nemusí promiskuitní režim fungovat

Tento kód demonstruje základní princip síťového sniffování, ale v reálném nasazení by bylo vhodné doplnit:
Rozsáhlejší ošetření výjimek
Podrobnější analýzu paketů
Možnost filtrování
Ukládání do souboru
Vícevláknové zpracování
*/
