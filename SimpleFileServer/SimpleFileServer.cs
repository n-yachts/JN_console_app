using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class SimpleFileServer
{
    static async Task Main(string[] args)
    {
        int port = 8080;
        string shareDirectory = "./shared";

        Directory.CreateDirectory(shareDirectory);

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine($"File server běží na portu {port}");
        Console.WriteLine($"Sdílený adresář: {Path.GetFullPath(shareDirectory)}");

        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            _ = Task.Run(() => HandleClient(client, shareDirectory));
        }
    }

    static async Task HandleClient(TcpClient client, string shareDir)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
        {
            try
            {
                // Pošleme seznam souborů
                string[] files = Directory.GetFiles(shareDir);
                await writer.WriteLineAsync("Dostupné soubory:");
                foreach (string file in files)
                {
                    await writer.WriteLineAsync($"  {Path.GetFileName(file)}");
                }
                await writer.WriteLineAsync("END");
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"Chyba: {ex.Message}");
                await writer.FlushAsync();
            }
        }
    }
}