using System;  // Import základních systémových knihoven
using System.Net;  // Import knihoven pro práci se sítí (HTTP)
using System.Text;  // Import knihoven pro práci s textovými kódováními
using System.Threading.Tasks;  // Import knihoven pro asynchronní programování

class SimpleHTTPServer  // Hlavní třída HTTP serveru
{
    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod aplikace
    {
        int port = 8080;  // Nastavení čísla portu pro server
        HttpListener listener = new HttpListener();  // Vytvoření instance HTTP listeneru
        listener.Prefixes.Add($"http://localhost:{port}/");  // Přidání URL prefixu pro naslouchání
        listener.Start();  // Spuštění HTTP listeneru
        Console.WriteLine($"HTTP Server běží na portu {port}...");  // Informace o běhu serveru

        while (true)  // Nekonečná smyčka pro průběžné obsluhování požadavků
        {
            HttpListenerContext context = await listener.GetContextAsync();  // Asynchronní čekání na příchozí požadavek
            _ = Task.Run(() => ProcessRequest(context));  // Spuštění zpracování požadavku v novém vlákně
        }
    }

    static void ProcessRequest(HttpListenerContext context)  // Metoda pro zpracování HTTP požadavku
    {
        HttpListenerRequest request = context.Request;  // Získání informací o příchozím požadavku
        HttpListenerResponse response = context.Response;  // Příprava objektu pro HTTP odpověď

        // Vytvoření HTML odpovědi s informacemi o požadavku
        string responseString = $@"
<html>
  <body>
    <h1>Ahoj jsem HTTP server napsaný v C#!</h1>
    <p>Čas: {DateTime.Now}</p>
    <p>URL: {request.Url}</p>
    <p>Metoda: {request.HttpMethod}</p>
  </body>
</html>";

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);  // Převod řetězce na bajty v UTF-8
        response.ContentLength64 = buffer.Length;  // Nastavení hlavičky Content-Length
        response.OutputStream.Write(buffer, 0, buffer.Length);  // Zápis dat do výstupního streamu
        response.Close();  // Odeslání odpovědi a uzavření spojení
    }
}

/*
Inicializace serveru:
 Server naslouchá na http://localhost:8080/
 Používá třídu HttpListener z .NET knihovny
Příjem požadavků:
 Hlavní smyčka neustále čeká na příchozí požadavky pomocí GetContextAsync()
 Každý požadavek je zpracován asynchronně v samostatném tasku
Zpracování požadavku:
 Pro každý požadavek se generuje jednoduchá HTML stránka
 Stránka obsahuje:
  Pozdrav
  Aktuální čas a datum
  URL adresu požadavku
  Použitou HTTP metodu
Odeslání odpovědi:
 HTML obsah se převede na bajty
 Nastaví se správná délka obsahu
 Data se odešlou přes výstupní stream
 Spojení se uzavře
*/