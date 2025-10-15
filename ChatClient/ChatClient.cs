using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ChatClient
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Použití: ChatClient <server> <port>");
            return;
        }

        // using TcpClient client = new TcpClient();
        using (var client = new TcpClient())
        {
            await client.ConnectAsync(args[0], int.Parse(args[1]));
            Console.WriteLine($"Připojeno k {args[0]}:{args[1]}");

            _ = Task.Run(() => ReceiveMessages(client));

            while (true)
            {
                string input = Console.ReadLine();
                if (input == "exit") break;

                byte[] data = Encoding.UTF8.GetBytes(input);
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
        }
    }

    static async Task ReceiveMessages(TcpClient client)
    {
        byte[] buffer = new byte[1024];
        NetworkStream stream = client.GetStream();

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Server: {message}");
        }
    }
}