using System;  // Základní jmenný prostor pro Console, Exception a základní třídy
using System.Net.Security;  // SslStream a SslPolicyErrors pro TLS/SSL komunikaci
using System.Security.Cryptography.X509Certificates;  // Práce s X.509 certifikáty

class SSLChecker
{
    static void Main(string[] args)
    {
        // Kontrola počtu argumentů - program vyžaduje přesně jeden argument
        if (args.Length != 1)
        {
            // Výpis návodu k použití pokud není zadán správný počet argumentů
            Console.WriteLine("Použití: SSLChecker <hostname:port>");
            Console.WriteLine("Příklad: SSLChecker google.com:443");
            return; // Ukončení programu
        }

        // Zpracování vstupního argumentu
        // Rozdělení řetězce na části podle dvojtečky (např. "google.com:443")
        string[] hostParts = args[0].Split(':');
        string hostname = hostParts[0];  // První část - název hostitele
        // Druhá část - port převedený na číslo (bez portu se použije výchozí HTTPS port 443)
        int port = hostParts.Length > 1 ? int.Parse(hostParts[1]) : 443;

        // Vytvoření TCP připojení k zadanému serveru a portu
        using var client = new System.Net.Sockets.TcpClient(hostname, port);

        // Vytvoření SSL streamu pro zabezpečenou komunikaci
        using var sslStream = new SslStream(
            client.GetStream(),  // Základní síťový stream z TCP klienta
            false,  // Nenechávat vnitřní stream otevřený po zničení SSL streamu
            ValidateServerCertificate,  // Callback funkce pro validaci certifikátu
            null  // Context pro callback (nepoužívá se)
        );

        try
        {
            // Provedení SSL/TLS handshake (certifikát se ověřuje callbackem ValidateServerCertificate, který v ukázce přijme vše)
            sslStream.AuthenticateAsClient(hostname);

            // Získání certifikátu ze serveru
            var certificate = sslStream.RemoteCertificate;

            // Konverze na X509Certificate2 pro pokročilejší operace
            var x509 = new X509Certificate2(certificate);

            // Výpis informací o certifikátu
            Console.WriteLine($"SSL Certifikát pro {hostname}:");
            Console.WriteLine($"Subjekt: {x509.Subject}");  // Vlastník certifikátu
            Console.WriteLine($"Vydavatel: {x509.Issuer}");  // Certifikační autorita
            Console.WriteLine($"Platný od: {x509.NotBefore}");  // Začátek platnosti
            Console.WriteLine($"Platný do: {x509.NotAfter}");  // Konec platnosti
            Console.WriteLine($"SHA1: {x509.GetCertHashString()}");  // Otisk certifikátu
        }
        catch (Exception ex)
        {
            // Zachycení a výpis chyb (např. neplatný certifikát, problémy s připojením)
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    // Callback metoda pro validaci serverového certifikátu
    static bool ValidateServerCertificate(
        object sender,  // Objekt, který vyvolal callback
        X509Certificate certificate,  // Certifikát serveru
        X509Chain chain,  // Řetěz certifikátů
        SslPolicyErrors sslPolicyErrors)  // Zjištěné chyby v certifikátu
    {
        // PRO UKÁZKU - přijímá všechny certifikáty bez ohledu na chyby
        // V reálném použití by zde měla být řádná kontrola platnosti
        return true;
    }
}

/*
Vstup: hostname nebo hostname:port (bez portu se použije 443)
Navázání spojení:
 TcpClient otevře TCP spojení, SslStream nad ním provede TLS handshake
 AuthenticateAsClient(hostname) odešle jméno serveru (SNI), podle kterého server vybere certifikát
Výpis certifikátu:
 Subjekt (komu byl vydán), vydavatel (certifikační autorita), doba platnosti a SHA1 otisk
Validace:
 Callback ValidateServerCertificate vždy vrací true, takže se vypíše i neplatný nebo self-signed certifikát
 To je vhodné pro diagnostiku, ne pro bezpečné připojení
*/