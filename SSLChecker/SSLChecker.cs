using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

class SSLChecker
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: SSLChecker <hostname:port>");
            Console.WriteLine("Příklad: SSLChecker google.com:443");
            return;
        }

        string hostname = args[0].Split(':')[0];
        int port = int.Parse(args[0].Split(':')[1]);

        using var client = new System.Net.Sockets.TcpClient(hostname, port);
        using var sslStream = new SslStream(client.GetStream(), false,
            ValidateServerCertificate, null);

        try
        {
            sslStream.AuthenticateAsClient(hostname);
            var certificate = sslStream.RemoteCertificate;
            var x509 = new X509Certificate2(certificate);

            Console.WriteLine($"SSL Certifikát pro {hostname}:");
            Console.WriteLine($"Subjekt: {x509.Subject}");
            Console.WriteLine($"Vydavatel: {x509.Issuer}");
            Console.WriteLine($"Platný od: {x509.NotBefore}");
            Console.WriteLine($"Platný do: {x509.NotAfter}");
            Console.WriteLine($"SHA1: {x509.GetCertHashString()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static bool ValidateServerCertificate(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true; // Přijmout všechny certifikáty pro testování
    }
}