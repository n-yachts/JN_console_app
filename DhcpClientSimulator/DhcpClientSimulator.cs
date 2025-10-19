using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class DhcpClient
{
    static void Main()
    {
        Console.WriteLine("DHCP Client Simulator\n");

        try
        {
            // Vytvoření DHCP discover zprávy
            byte[] discoverPacket = CreateDhcpDiscover();

            // Vytvoření UDP klienta pro komunikaci
            using (UdpClient client = new UdpClient())
            {
                // Povolení broadcastu pro odesílání do sítě
                client.EnableBroadcast = true;

                // Nastavení cílového endpointu (DHCP server port 67)
                IPEndPoint dhcpEndpoint = new IPEndPoint(IPAddress.Broadcast, 67);

                Console.WriteLine("Odesílám DHCP Discover...");
                // Odeslání DHCP discover zprávy
                client.Send(discoverPacket, discoverPacket.Length, dhcpEndpoint);

                // Nastavení timeoutu pro příjem odpovědi (5 sekund)
                client.Client.ReceiveTimeout = 5000;

                try
                {
                    // Příprava proměnné pro přijatá data
                    IPEndPoint responseEndpoint = new IPEndPoint(IPAddress.Any, 0);

                    // Čekání na přijetí odpovědi
                    byte[] responseData = client.Receive(ref responseEndpoint);

                    Console.WriteLine($"Obdržena odpověď od {responseEndpoint}");

                    // Zpracování přijaté DHCP odpovědi
                    ParseDhcpResponse(responseData);
                }
                catch (SocketException)
                {
                    Console.WriteLine("Žádná odpověď od DHCP serveru (timeout)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static byte[] CreateDhcpDiscover()
    {
        // Inicializace DHCP paketu s dostatečnou velikostí
        byte[] packet = new byte[300];
        Random random = new Random();

        // DHCP hlavička
        packet[0] = 0x01; // OP: Boot Request (klient -> server)
        packet[1] = 0x01; // HTYPE: Ethernet (10Mb)
        packet[2] = 0x06; // HLEN: Hardware address length = 6 (MAC adresa)
        packet[3] = 0x00; // HOPS: Počet relay agentů = 0 (přímá komunikace)

        // Transaction ID (náhodné číslo pro spojení request/response)
        byte[] xid = BitConverter.GetBytes(random.Next());
        Array.Copy(xid, 0, packet, 4, 4);

        // Flags: Broadcast flag = 1 (server může odpovědět broadcastem)
        packet[10] = 0x80;
        packet[11] = 0x00;

        // Client MAC address (simulovaná náhodná MAC adresa)
        for (int i = 0; i < 6; i++)
            packet[28 + i] = (byte)random.Next(256);

        // Magic cookie: identifikace DHCP options
        packet[236] = 0x63;
        packet[237] = 0x82;
        packet[238] = 0x53;
        packet[239] = 0x63;

        // DHCP Options začínají na pozici 240
        int optionIndex = 240;

        // DHCP Message Type Option (53)
        packet[optionIndex++] = 53; // Option code = 53 (DHCP Message Type)
        packet[optionIndex++] = 1;  // Length = 1 byte
        packet[optionIndex++] = 1;  // Value = 1 (DHCP Discover)

        // Parameter Request List Option (55)
        packet[optionIndex++] = 55; // Option code = 55 (Parameter Request List)
        packet[optionIndex++] = 8;  // Length = 8 bytů
        packet[optionIndex++] = 1;  // Requested option: 1 = Subnet Mask
        packet[optionIndex++] = 3;  // 3 = Router
        packet[optionIndex++] = 6;  // 6 = Domain Name Server
        packet[optionIndex++] = 15; // 15 = Domain Name
        packet[optionIndex++] = 28; // 28 = Broadcast Address
        packet[optionIndex++] = 51; // 51 = IP Address Lease Time
        packet[optionIndex++] = 58; // 58 = Renewal Time
        packet[optionIndex++] = 59; // 59 = Rebinding Time

        // End Option (255) - konec options sekce
        packet[optionIndex] = 255;

        return packet;
    }

    static void ParseDhcpResponse(byte[] data)
    {
        // Kontrola minimální délky DHCP paketu
        if (data.Length < 240) return;

        // Hledání DHCP Message Type option (53)
        for (int i = 240; i < data.Length - 2; i++)
        {
            if (data[i] == 53) // Nalezena option 53
            {
                byte messageType = data[i + 2];
                string typeName = messageType switch
                {
                    1 => "DISCOVER",
                    2 => "OFFER",
                    3 => "REQUEST",
                    4 => "DECLINE",
                    5 => "ACK",
                    6 => "NAK",
                    7 => "RELEASE",
                    _ => "UNKNOWN"
                };
                Console.WriteLine($"DHCP Message Type: {typeName} ({messageType})");
                break;
            }
        }

        // Čtení navrhované IP adresy z pole YOUR IP ADDRESS
        byte[] yourIp = new byte[4];
        Array.Copy(data, 16, yourIp, 0, 4);
        Console.WriteLine($"Navrhovaná IP: {new IPAddress(yourIp)}");

        // Čtení IP adresy DHCP serveru
        byte[] serverIp = new byte[4];
        Array.Copy(data, 20, serverIp, 0, 4);
        Console.WriteLine($"DHCP Server: {new IPAddress(serverIp)}");
    }
}

/*
Struktura DHCP paketu:
 První 240 bytů tvoří pevnou hlavičku (BOOTP)
 Magic cookie (0x63825363) odděluje hlavičku od options
 Options obsahují specifické DHCP informace
DHCP Message Types:
 1 = DISCOVER (klient hledá servery)
 2 = OFFER (server nabízí IP)
 5 = ACK (potvrzení přidělení)
Důležité pozice v hlavičce:
 Byte 16-19: YOUR IP ADDRESS
 Byte 20-23: SERVER IP ADDRESS
 Byte 28-33: CLIENT MAC ADDRESS
Network Communication:
 Klient posílá broadcast na port 67
 Server odpovídá na port 68
 Používá se UDP protokol
Tento kód simuluje základní DHCP komunikaci, kde klient pošle discover zprávu a zpracuje odpověď od serveru.
V reálném prostředí by byla implementace komplexnější včetně handlingu různých DHCP options a stavového stroje.
*/