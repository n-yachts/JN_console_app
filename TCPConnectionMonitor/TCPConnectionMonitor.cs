using System;
using System.Net;
using System.Net.NetworkInformation;

class TCPConnectionMonitor
{
    static void Main()
    {
        Console.WriteLine("Aktivní TCP spojení:\n");

        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
        TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();

        foreach (TcpConnectionInformation c in connections)
        {
            Console.WriteLine($"Místní: {c.LocalEndPoint} -> Vzdálené: {c.RemoteEndPoint} | Stav: {c.State}");
        }
    }
}