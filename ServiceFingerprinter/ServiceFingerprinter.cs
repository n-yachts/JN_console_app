using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ServiceFingerprinter
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Použití: ServiceFingerprinter <host> <port>");
            return;
        }

        string host = args[0];
        int port = int.Parse(args[1]);

        using (TcpClient client = new TcpClient())
        {
            try
            {
                await client.ConnectAsync(host, port);
                NetworkStream stream = client.GetStream();

                // Čekáme na banner
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string banner = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"Banner služby na {host}:{port}:");
                Console.WriteLine(banner);

                // Pro HTTP služby pošleme HTTP request
                if (port == 80 || port == 443 || port == 8080)
                {
                    byte[] httpRequest = Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\n\r\n");
                    await stream.WriteAsync(httpRequest, 0, httpRequest.Length);

                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string httpResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine("\nHTTP odpověď:");
                    Console.WriteLine(httpResponse);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }
    }
}