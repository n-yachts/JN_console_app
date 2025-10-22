using System;  // Importování základních systémových knihoven
using System.Net.Sockets;  // Importování knihoven pro práci se síťovými sokety
using System.Text;  // Importování knihoven pro práci s textovými kódováními
using System.Threading.Tasks;  // Importování knihoven pro asynchronní programování

class TrafficGenerator  // Definice třídy pro generování síťového provozu
{
    static async Task Main(string[] args)  // Hlavní asynchronní metoda programu
    {
        // Kontrola počtu argumentů příkazové řádky
        if (args.Length != 3)
        {
            // Výpis správného použití pokud počet argumentů neodpovídá
            Console.WriteLine("Použití: TrafficGenerator <target> <port> <packets>");
            return;  // Předčasné ukončení programu
        }

        string target = args[0];  // Načtení cílové IP adresy nebo hostname z prvního argumentu
        int port = int.Parse(args[1]);  // Převod druhého argumentu na číslo portu
        int packets = int.Parse(args[2]);  // Převod třetího argumentu na počet paketů

        // Vytvoření UDP klienta pomocí using pro automatické uvolnění prostředků
        using (UdpClient client = new UdpClient())
        {
            // Převod textové zprávy na pole bajtů v UTF-8 kódování
            byte[] data = Encoding.UTF8.GetBytes("Test packet");

            // Cyklus pro odeslání zadaného počtu paketů
            for (int i = 0; i < packets; i++)
            {
                // Asynchronní odeslání UDP paketu na cílovou adresu a port
                await client.SendAsync(data, data.Length, target, port);

                // Výpis informace o odeslaném paketu s číslem pořadí
                Console.WriteLine($"Odeslán packet {i + 1}/{packets}");

                // Čekání 100 ms před odesláním dalšího paketu
                await Task.Delay(100);
            }
        }
    }
}

/*
Kontrola argumentů: Program vyžaduje přesně 3 argumenty - cílovou adresu, port a počet paketů.
Parsování argumentů:
 target - cílový server (IP nebo doménové jméno)
 port - číslo cílového portu
 packets - celkový počet UDP paketů k odeslání
Práce s UDP klientem:
 Používá UdpClient pro síťovou komunikaci
 Data se převádějí do bajtového pole
 Pro každý paket se volá asynchronní metoda SendAsync
Řízení toku:
 Po každém odeslaném paketu následuje 100ms pauza
 Průběžné výpisy do konzole informují o stavu odesílání
Resource Management:
 using statement zajišťuje správné uvolnění síťových prostředků

Tento program slouží jako nástroj pro generování testovacího síťového provozu pomocí UDP protokolu. Lze jej využít pro testování síťových aplikací, měření výkonu nebo simulaci síťové zátěže.
*/