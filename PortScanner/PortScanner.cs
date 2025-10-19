using System;  // Importování základních systémových knihoven pro práci s konzolí a základními funkcemi
using System.Net;  // Knihovna pro síťové funkce (DNS, IP adresy)
using System.Net.Sockets;  // Knihovna pro práci se síťovými sokety (TCP/UDP)
using System.Threading.Tasks;  // Knihovna pro asynchronní programování (Task, async/await)

class PortScanner  // Definice třídy pro skenování portů
{
    static async Task Main(string[] args)  // Hlavní asynchronní metoda programu
    {
        // Kontrola počtu argumentů - pokud není zadán přesně jeden argument, program skončí
        if (args.Length != 1)
        {
            // Výpis správného použití programu
            Console.WriteLine("Použití: PortScanner <hostname/IP>");
            return;  // Předčasné ukončení programu
        }

        string target = args[0];  // Uložení prvního argumentu jako cílový hostitel/IP adresu

        // Pole běžných portů k testování (FTP, SSH, Telnet, SMTP, DNS, HTTP, POP3, IMAP, HTTPS atd.)
        int[] commonPorts = { 21, 22, 23, 25, 53, 80, 110, 143, 443, 993, 995 };

        // Cyklus procházející všechny porty v poli commonPorts
        foreach (int port in commonPorts)
        {
            // Vytvoření disposable TCP klienta (automaticky uvolní zdroje pomocí using)
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    // Pokus o asynchronní připojení k cíli na daný port
                    await client.ConnectAsync(target, port);

                    // Pokud připojení uspěje - port je otevřený
                    Console.WriteLine($"Port {port}: OTEVŘENÝ");
                }
                catch
                {
                    // Pokud dojde k chybě (timeout, odmítnutí) - port je zavřený/nedostupný
                    Console.WriteLine($"Port {port}: ZAVŘENÝ");
                }
            }  // Klient je automaticky uvolněn díky using statement
        }
    }
}

/*
Inicializace:
 Program kontroluje vstupní argumenty
 Načte cílovou adresu z příkazové řádky
 Definuje seznam běžných portů pro testování
Skenování:
 Pro každý port vytvoří nový TCP klient
 Pokusí se asynchronně připojit k cíli
 Výsledek připojení se vypíše do konzole
Zpracování výsledků:
 Úspěšné připojení = otevřený port
 Výjimka = zavřený/neodpovídající port
 Automatické uvolnění síťových prostředků
Typické porty ve skenu:
 21 (FTP), 22 (SSH), 23 (Telnet)
 25 (SMTP), 53 (DNS), 80 (HTTP)
 110 (POP3), 143 (IMAP), 443 (HTTPS)
 993 (IMAPS), 995 (POP3S)
*/