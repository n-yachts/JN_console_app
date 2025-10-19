using System;  // Import základních systémových funkcí a tříd
using System.Net.Security;  // Import pro práci s SSL/TStream
using System.Security.Cryptography.X509Certificates;  // Import pro práci s X.509 certifikáty

class CertificateExpiryChecker  // Hlavní třída pro kontrolu platnosti certifikátu
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        if (args.Length != 1)  // Kontrola počtu argumentů
        {
            // Výpis správného použití programu
            Console.WriteLine("Použití: CertificateExpiryChecker <hostname:port>");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        // Rozdělení vstupního argumentu na části podle dvojtečky
        string[] parts = args[0].Split(':');
        string hostname = parts[0];  // Extrakce názvu serveru z první části
        // Extrakce portu (pokud není zadán, použije se výchozí HTTPS port 443)
        int port = parts.Length > 1 ? int.Parse(parts[1]) : 443;

        try  // Zachycení potenciálních chyb při připojování
        {
            // Vytvoření TCP připojení k cílovému serveru
            using (var client = new System.Net.Sockets.TcpClient(hostname, port))
            // Vytvoření SSL proudu z TCP připojení
            using (var sslStream = new SslStream(client.GetStream(), false,
                // Callback pro ověření certifikátu (vždy vrací true = přijímá všechny certifikáty)
                (sender, certificate, chain, sslPolicyErrors) => true))
            {
                // Ověření klienta na serveru (provede SSL handshake)
                sslStream.AuthenticateAsClient(hostname);
                // Získání serverového certifikátu a vytvoření objektu pro práci s ním
                var cert = new X509Certificate2(sslStream.RemoteCertificate);

                DateTime expiry = cert.NotAfter;  // Získání data expirace certifikátu
                TimeSpan remaining = expiry - DateTime.Now;  // Výpočet zbývající platnosti

                // Výpis informací o certifikátu
                Console.WriteLine($"Certifikát pro {hostname}:");
                Console.WriteLine($"  Subjekt: {cert.Subject}");  // Vlastník certifikátu
                Console.WriteLine($"  Vydavatel: {cert.Issuer}");  // Certifikační autorita
                Console.WriteLine($"  Platný do: {expiry:dd.MM.yyyy}");  // Datum expirace
                Console.WriteLine($"  Zbývá: {remaining.Days} dnů");  // Počet zbývajících dnů

                if (remaining.Days < 30)  // Kontrola, zda certifikát expiruje za méně než 30 dní
                    Console.WriteLine("  ⚠️  Certifikát brzy expiruje!");  // Varování
            }
        }
        catch (Exception ex)  // Zachycení jakékoliv výjimky
        {
            // Výpis chybové zprávy
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}

/*
Program přijímá všechny certifikáty (i neplatné/self-signed)
Je vhodný pro základní diagnostiku, ne pro produkční použití
Upozorní na certifikáty expirující za méně než 30 dní

Podrobný popis funkce:
Načtení potřebných knihoven - Práce se sítí, SSL streamy a kryptografií
Zpracování argumentů - Program očekává jako vstup hostname:port
Navázání spojení - Vytvoří TCP připojení k cílovému serveru
SSL handshake - Provede ověření SSL certifikátu
Analýza certifikátu - Získá informace o platnosti a vlastníkovi
Vyhodnocení - Vypíše datum expirace a varuje při brzké expiraci
Ošetření chyb - Zachytává chyby spojení a neplatné certifikáty
*/