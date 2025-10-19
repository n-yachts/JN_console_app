using System;  // Základní jmenný prostor pro vstupy/výstupy, řetězce atd.
using System.Net;  // Obsahuje třídy pro síťovou komunikaci (IPAddress)
using System.Net.Sockets;  // Poskytuje TCP/IP funkce (TcpListener, TcpClient)
using System.Text;  // Práce s textovými kodováními (Encoding.UTF8)
using System.Threading.Tasks;  // Podpora asynchronních operací (Task, async/await)

class ChatServer  // Hlavní třída chatovacího serveru
{
    static async Task Main()  // Hlavní asynchronní vstupní bod aplikace
    {
        // Vytvoření TCP listeneru naslouchajícího na všech síťových rozhraních port 8080
        TcpListener listener = new TcpListener(IPAddress.Any, 8080);

        listener.Start();  // Spuštění naslouchání příchozích spojení
        Console.WriteLine("Chat server běží na portu 8080...");  // Informace o běhu serveru

        while (true)  // Nekonečná smyčka pro průběžné přijímání klientů
        {
            // Asynchronní čekání na příchozí připojení (blokující operace)
            TcpClient client = await listener.AcceptTcpClientAsync();

            // Spuštění nového úkolu pro obsluhu klienta (bez čekání na dokončení)
            _ = Task.Run(() => HandleClient(client));
        }
    }

    static async Task HandleClient(TcpClient client)  // Metoda pro obsluhu jednotlivého klienta
    {
        // Získání síťového streamu pro čtení/zápis dat
        NetworkStream stream = client.GetStream();

        // Vyrovnávací paměť pro příchozí data (velikost 1 KB)
        byte[] buffer = new byte[1024];

        // Výpis informace o připojeném klientovi (IP adresa a port)
        Console.WriteLine($"Klient připojen: {client.Client.RemoteEndPoint}");

        while (true)  // Smyčka pro průběžné čtení zpráv od klienta
        {
            // Asynchronní čtení dat ze streamu (počet přečtených bytů)
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead == 0) break;  // Klient ukončil spojení (prázdný packet)

            // Převod přijatých bytů na řetězec pomocí UTF-8 kodování
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

            // Výpis zprávy s identifikací klienta
            Console.WriteLine($"[{client.Client.RemoteEndPoint}]: {message}");
        }

        // Informace o odpojení klienta
        Console.WriteLine($"Klient odpojen: {client.Client.RemoteEndPoint}");

        client.Close();  // Uzavření spojení a uvolnění prostředků
    }
}

/*
Server naslouchá na všech síťových rozhraních na portu 8080
Pro každého nového klienta vytvoří samostatnou úlohu
Čte zprávy v UTF-8 kódování
Zobrazuje zprávy v konzoli s informací o odesílateli
Automaticky detekuje odpojení klienta
Používá asynchronní operace pro efektivní využití prostředků

Server neodesílá odpovědi klientům
Neobsahuje mechanismus pro broadcast zpráv mezi klienty
Maximální velikost jedné zprávy je 1024 bytů
*/