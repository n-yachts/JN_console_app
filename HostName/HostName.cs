using System;  // Základní jmenný prostor pro Console, Exception a základní třídy
using System.Net;  // Třídy pro práci se sítí (IPAddress, Dns, IPHostEntry)
using System.Threading;  // CancellationTokenSource pro zrušení časovače
using System.Threading.Tasks;  // Podpora asynchronního programování (Task, async/await)

namespace HostName  // Jmenný prostor pro organizaci kódu
{
    class HostName  // Hlavní třída programu
    {
        static async Task Main(string[] args)  // Asynchronní vstupní bod programu
        {
            Console.WriteLine("=== Získání HostName z IP adresy ===\n");

            // Kontrola počtu argumentů - program vyžaduje přesně jeden argument
            if (args.Length != 1)
            {
                Console.WriteLine("Použití: HostName <IPv4 adresa>");
                return;  // Ukončení programu při chybném počtu argumentů
            }

            string ipAddress = args[0];  // Uložení IP adresy z prvního argumentu

            // Zdroj tokenu pro časovač - po dokončení dotazu se časovač zruší, aby nezůstal běžet na pozadí
            using (var cts = new CancellationTokenSource())
            {
                try  // Ošetření chyb při parsování adresy a DNS dotazu
                {
                    // Převod textu na objekt IPAddress (vyvolá FormatException u neplatné adresy)
                    IPAddress ip = IPAddress.Parse(ipAddress);

                    // Spuštění reverzního DNS dotazu (PTR záznam) bez čekání na výsledek
                    var task = Dns.GetHostEntryAsync(ip);

                    // Čekání na dokončení dotazu nebo na uplynutí časového limitu 5 sekund (podle toho, co nastane dřív)
                    var completedTask = await Task.WhenAny(task, Task.Delay(5000, cts.Token));

                    if (completedTask == task)  // Dotaz skončil dřív než časový limit
                    {
                        // Získání výsledku (případná chyba dotazu se vyvolá zde jako výjimka)
                        IPHostEntry hostEntry = await task;
                        Console.WriteLine($"Název počítače: {hostEntry.HostName}");
                    }
                    else  // Vypršel časový limit
                    {
                        Console.WriteLine("Časový limit vypršel - server neodpovídá");
                    }

                    cts.Cancel();  // Zrušení časovače Task.Delay, který už není potřeba
                }
                catch (Exception ex)  // Zachycení všech výjimek (neplatná adresa, DNS chyba atd.)
                {
                    Console.WriteLine($"Chyba: {ex.Message}");
                }
            }
        }
    }
}

/*
Reverzní DNS dotaz:
 Dns.GetHostEntryAsync(IPAddress) zjistí jméno počítače k IP adrese (PTR záznam)
Časový limit:
 Task.WhenAny čeká na první dokončenou úlohu - dotaz nebo Task.Delay(5000)
 Dns.GetHostEntryAsync nemá vlastní časový limit, proto se používá takto
Ošetření chyb:
 Neplatná IP adresa i chyba DNS skončí v bloku catch
*/
