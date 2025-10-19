using System;  // Importování základních systémových knihoven
using System.Net;  // Importování knihoven pro síťovou komunikaci
using System.Net.NetworkInformation;  // Importování knihoven pro ping funkcionalitu
using System.Threading.Tasks;  // Importování knihoven pro asynchronní operace

class CustomPing  // Definice třídy pro pingování
{
    static async Task Main(string[] args)  // Hlavní asynchronní metoda programu
    {
        if (args.Length != 1)  // Kontrola počtu argumentů příkazové řádky
        {
            Console.WriteLine("Použití: CustomPing <hostname/IP>");  // Chybová zpráva při špatném počtu argumentů
            return;  // Předčasné ukončení programu
        }

        string target = args[0];  // Uložení cílové adresy z prvního argumentu
        Ping ping = new Ping();  // Vytvoření instance ping klienta

        for (int i = 0; i < 4; i++)  // Cyklus pro 4 ping požadavky
        {
            try  // Blok pro zachycení výjimek
            {
                // Asynchronní odeslání ping požadavku s timeoutem 1000ms
                PingReply reply = await ping.SendPingAsync(target, 1000);

                // Výpis úspěšné odpovědi s detaily
                Console.WriteLine($"Odpověď od {reply.Address}: bytes={reply.Buffer.Length} time={reply.RoundtripTime}ms TTL={reply.Options.Ttl}");
            }
            catch (Exception ex)  // Zachycení jakékoliv výjimky
            {
                Console.WriteLine($"Chyba: {ex.Message}");  // Výpis chybové zprávy
            }
            await Task.Delay(1000);  // Čekání 1 sekundu mezi jednotlivými pingy
        }
    }
}

/*
Program kontroluje správný počet vstupních argumentů
Cílová adresa se načte z příkazové řádky
Používá asynchronní operace pro neblokující síťovou komunikaci
Odesílá 4 ping požadavky s vteřinovými intervaly
Pro každou odpověď vypisuje:
IP adresu cíle
Velikost odpovědi
Čas odezvy (RTT)
Hodnotu TTL
Obsahuje kompletní ošetření chyb
Používá moderní asynchronní přístup s async/await
*/