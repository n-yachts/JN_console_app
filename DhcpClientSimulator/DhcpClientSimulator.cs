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

            using (UdpClient client = new UdpClient())
            {
                client.EnableBroadcast = true;
                IPEndPoint dhcpEndpoint = new IPEndPoint(IPAddress.Broadcast, 67);

                Console.WriteLine("Odesílám DHCP Discover...");
                client.Send(discoverPacket, discoverPacket.Length, dhcpEndpoint);

                // Čekání na odpověď
                client.Client.ReceiveTimeout = 5000;

                try
                {
                    IPEndPoint responseEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    byte[] responseData = client.Receive(ref responseEndpoint);

                    Console.WriteLine($"Obdržena odpověď od {responseEndpoint}");
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
        byte[] packet = new byte[240];
        Random random = new Random();

        // DHCP header
        packet[0] = 0x01; // OP: Boot Request
        packet[1] = 0x01; // HTYPE: Ethernet
        packet[2] = 0x06; // HLEN: 6
        packet[3] = 0x00; // HOPS: 0

        // Transaction ID
        byte[] xid = BitConverter.GetBytes(random.Next());
        Array.Copy(xid, 0, packet, 4, 4);

        // Flags (Broadcast)
        packet[10] = 0x80;
        packet[11] = 0x00;

        // Client MAC address (náhodná)
        for (int i = 0; i < 6; i++)
            packet[28 + i] = (byte)random.Next(256);

        // Magic cookie
        packet[236] = 0x63;
        packet[237] = 0x82;
        packet[238] = 0x53;
        packet[239] = 0x63;

        // DHCP Options
        int optionIndex = 240;

        // DHCP Message Type: Discover
        packet[optionIndex++] = 53; // Option code
        packet[optionIndex++] = 1;  // Length
        packet[optionIndex++] = 1;  // Discover

        // Parameter Request List
        packet[optionIndex++] = 55; // Option code
        packet[optionIndex++] = 8;  // Length
        packet[optionIndex++] = 1;  // Subnet Mask
        packet[optionIndex++] = 3;  // Router
        packet[optionIndex++] = 6;  // Domain Name Server
        packet[optionIndex++] = 15; // Domain Name
        packet[optionIndex++] = 28; // Broadcast Address
        packet[optionIndex++] = 51; // IP Address Lease Time
        packet[optionIndex++] = 58; // Renewal Time
        packet[optionIndex++] = 59; // Rebinding Time

        // End option
        packet[optionIndex] = 255;

        return packet;
    }

    static void ParseDhcpResponse(byte[] data)
    {
        if (data.Length < 240) return;

        // DHCP Message Type
        for (int i = 240; i < data.Length - 2; i++)
        {
            if (data[i] == 53) // DHCP Message Type option
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

        // Your IP Address
        byte[] yourIp = new byte[4];
        Array.Copy(data, 16, yourIp, 0, 4);
        Console.WriteLine($"Navrhovaná IP: {new IPAddress(yourIp)}");

        // Server IP Address
        byte[] serverIp = new byte[4];
        Array.Copy(data, 20, serverIp, 0, 4);
        Console.WriteLine($"DHCP Server: {new IPAddress(serverIp)}");
    }
}