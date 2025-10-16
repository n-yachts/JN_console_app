using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

class CertificateExpiryChecker
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: CertificateExpiryChecker <hostname:port>");
            return;
        }

        string[] parts = args[0].Split(':');
        string hostname = parts[0];
        int port = parts.Length > 1 ? int.Parse(parts[1]) : 443;

        try
        {
            using (var client = new System.Net.Sockets.TcpClient(hostname, port))
            using (var sslStream = new SslStream(client.GetStream(), false,
                (sender, certificate, chain, sslPolicyErrors) => true))
            {
                sslStream.AuthenticateAsClient(hostname);
                var cert = new X509Certificate2(sslStream.RemoteCertificate);

                DateTime expiry = cert.NotAfter;
                TimeSpan remaining = expiry - DateTime.Now;

                Console.WriteLine($"Certifikát pro {hostname}:");
                Console.WriteLine($"  Subjekt: {cert.Subject}");
                Console.WriteLine($"  Vydavatel: {cert.Issuer}");
                Console.WriteLine($"  Platný do: {expiry:dd.MM.yyyy}");
                Console.WriteLine($"  Zbývá: {remaining.Days} dnů");

                if (remaining.Days < 30)
                    Console.WriteLine("  ⚠️  Certifikát brzy expiruje!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}