using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class WhoisClient
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: WhoisClient <domain>");
            return;
        }

        string domain = args[0];
        string whoisServer = "whois.iana.org";
        int port = 43;

        try
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(whoisServer, port);

            using NetworkStream stream = client.GetStream();
            byte[] request = Encoding.ASCII.GetBytes(domain + "\r\n");
            await stream.WriteAsync(request, 0, request.Length);

            byte[] buffer = new byte[4096];
            int bytesRead;
            StringBuilder response = new StringBuilder();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            }

            Console.WriteLine($"WHOIS informace pro {domain}:");
            Console.WriteLine(response.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}