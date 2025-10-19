using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ChatClient
{
    static async Task Main(string[] args)
    {
        // Kontrola počtu argumentů - program vyžaduje IP adresu serveru a číslo portu
        if (args.Length != 2)
        {
            Console.WriteLine("Použití: ChatClient <server> <port>");
            return;
        }

        // Vytvoření TCP klienta pomocí using pro automatické uvolnění prostředků
        using (var client = new TcpClient())
        {
            // Asynchronní připojení k serveru s parametry z příkazové řádky
            await client.ConnectAsync(args[0], int.Parse(args[1]));
            Console.WriteLine($"Připojeno k {args[0]}:{args[1]}");

            // Spuštění samostatné úlohy pro příjem zpráv bez blokování hlavního vlákna
            _ = Task.Run(() => ReceiveMessages(client));

            // Hlavní smyčka pro odesílání zpráv
            while (true)
            {
                // Načtení vstupu od uživatele
                string input = Console.ReadLine();

                // Ukončení klienta při zadání "exit"
                if (input == "exit") break;

                // Převod zprávy na bajty a odeslání přes síťový stream
                byte[] data = Encoding.UTF8.GetBytes(input);
                await client.GetStream().WriteAsync(data, 0, data.Length);
            }
        }
    }

    static async Task ReceiveMessages(TcpClient client)
    {
        // Buffer pro ukládání přijatých dat
        byte[] buffer = new byte[1024];
        NetworkStream stream = client.GetStream();

        while (true)
        {
            // Asynchronní čtení dat ze serveru
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            // Ukončení smyčky při odpojení serveru
            if (bytesRead == 0) break;

            // Převod přijatých bajtů na řetězec a výpis
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Server: {message}");
        }
    }
}

/*
Inicializace klienta:
 Kontrola vstupních parametrů (adresa serveru a port)
 Použití TcpClient pro síťovou komunikaci
Připojení k serveru:
 Asynchronní operace ConnectAsync
 Automatické parsování portu z argumentů
Zpracování zpráv:
 Hlavní vlákno: čtení vstupu z konzole a odesílání
 Vedlejší vlákno: příjem zpráv ze serveru
 Použití Task.Run pro paralelní zpracování
Síťová komunikace:
 Kódování/zpětný rozklad zpráv do UTF-8
 Práce se síťovým streamem pomocí NetworkStream
 Asynchronní operace pro neblokující IO
Ukončení spojení:
 Klíčové slovo using zajišťuje správné uvolnění prostředků
 Ukončení při zadání "exit" nebo odpojení serveru
*/