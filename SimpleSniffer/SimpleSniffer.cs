using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class SimpleSniffer
{
    static void Main()
    {
        Console.WriteLine("Základní síťový sniffer - zachytává ICMP a TCP pakety\n");

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
        socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

        byte[] inValue = BitConverter.GetBytes(1);
        byte[] outValue = BitConverter.GetBytes(0);
        socket.IOControl(IOControlCode.ReceiveAll, inValue, outValue);

        byte[] buffer = new byte[4096];

        while (true)
        {
            int bytesRead = socket.Receive(buffer);
            if (bytesRead > 0)
            {
                IPAddress sourceIP = new IPAddress(BitConverter.ToUInt32(buffer, 12));
                IPAddress destIP = new IPAddress(BitConverter.ToUInt32(buffer, 16));
                byte protocol = buffer[9];

                Console.WriteLine($"Paket: {sourceIP} -> {destIP} Protocol: {protocol} Velikost: {bytesRead} bytes");
            }
        }
    }
}