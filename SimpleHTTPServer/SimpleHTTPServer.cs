using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class SimpleHTTPServer
{
    static async Task Main(string[] args)
    {
        int port = 8080;
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Console.WriteLine($"HTTP Server běží na portu {port}...");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            _ = Task.Run(() => ProcessRequest(context));
        }
    }

    static void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string responseString = $@"
<html>
  <body>
    <h1>Hello from C# HTTP Server!</h1>
    <p>Čas: {DateTime.Now}</p>
    <p>URL: {request.Url}</p>
    <p>Metoda: {request.HttpMethod}</p>
  </body>
</html>";

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
}