using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class SipAnalyzer
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: SipAnalyzer <local_port>");
            Console.WriteLine("Příklad: SipAnalyzer 5060");
            return;
        }

        int port = int.Parse(args[0]);

        Console.WriteLine($"SIP Analyzer naslouchá na portu {port}...\n");
        Console.WriteLine("Stiskněte Ctrl+C pro ukončení.\n");

        await StartSipListener(port);
    }

    static async Task StartSipListener(int port)
    {
        using (UdpClient listener = new UdpClient(port))
        {
            while (true)
            {
                try
                {
                    UdpReceiveResult result = await listener.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    Console.WriteLine($"=== SIP Zpráva od {result.RemoteEndPoint} ===");
                    ParseSipMessage(message);
                    Console.WriteLine("=== Konec zprávy ===\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při přijímání: {ex.Message}");
                }
            }
        }
    }

    static void ParseSipMessage(string message)
    {
        string[] lines = message.Split('\n');

        if (lines.Length > 0)
        {
            // První řádek obsahuje metodu a URL
            string firstLine = lines[0].Trim();
            Console.WriteLine($"První řádek: {firstLine}");

            if (firstLine.StartsWith("SIP/2.0"))
            {
                Console.WriteLine("📨 SIP Response");
                string[] parts = firstLine.Split(' ');
                if (parts.Length >= 2)
                    Console.WriteLine($"Status: {parts[1]}");
            }
            else
            {
                Console.WriteLine("📤 SIP Request");
                string[] parts = firstLine.Split(' ');
                if (parts.Length >= 1)
                    Console.WriteLine($"Metoda: {parts[0]}");
            }
        }

        // Hlavičky
        Console.WriteLine("\nHlavičky:");
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) break;

            if (line.StartsWith("Via:") || line.StartsWith("From:") || line.StartsWith("To:") ||
                line.StartsWith("Call-ID:") || line.StartsWith("CSeq:") || line.StartsWith("Contact:"))
            {
                Console.WriteLine($"  {line}");
            }
        }

        // Tělo zprávy (SDP)
        bool inBody = false;
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line.Trim()))
            {
                inBody = true;
                continue;
            }

            if (inBody)
            {
                if (line.StartsWith("v=") || line.StartsWith("o=") || line.StartsWith("s=") ||
                    line.StartsWith("c=") || line.StartsWith("m=") || line.StartsWith("a="))
                {
                    Console.WriteLine($"SDP: {line}");
                }
            }
        }
    }
}