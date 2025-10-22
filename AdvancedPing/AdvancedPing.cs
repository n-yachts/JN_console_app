using System;  // Základní jmenný prostor pro vstupy/výstupy a základní třídy
using System.Net.NetworkInformation;  // Pro práci s pingem a síťovými informacemi
using System.Threading.Tasks;  // Pro asynchronní programování

namespace AdvancedPing  // Deklarace jmenného prostoru pro organizaci kódu
{
    class AdvancedPing  // Hlavní třída programu
    {
        static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
        {
            // Zpracování argumentů příkazového řádku
            if (args.Length == 0)  // Kontrola zda byly zadány nějaké argumenty
            {
                // Výpis nápovědy pokud nebyly zadány argumenty
                Console.WriteLine("Použití: AdvancedPing <adresa> [timeout] [počet pingů]");
                Console.WriteLine("Příklad: AdvancedPing google.com");
                Console.WriteLine("Příklad: AdvancedPing 192.168.1.1 500 10");
                return;  // Ukončení programu
            }

            string target = args[0];  // První argument je cílová adresa
            int timeout = 1000;  // Výchozí timeout 1000ms (1 sekunda)
            int pingCount = 4;   // Výchozí počet pingů

            // Zpracování druhého argumentu (timeout) pokud byl zadán
            if (args.Length > 1 && !int.TryParse(args[1], out timeout))
            {
                Console.WriteLine("Chyba: Timeout musí být číslo!");
                return;  // Ukončení programu při chybném formátu
            }

            // Zpracování třetího argumentu (počet pingů) pokud byl zadán
            if (args.Length > 2 && !int.TryParse(args[2], out pingCount))
            {
                Console.WriteLine("Chyba: Počet pingů musí být číslo!");
                return;  // Ukončení programu při chybném formátu
            }

            // Validace hodnot - kontrola že timeout a počet pingů jsou kladná čísla
            if (timeout < 1 || pingCount < 1)
            {
                Console.WriteLine("Chyba: Timeout a počet pingů musí být alespoň 1!");
                return;  // Ukončení programu při nevalidních hodnotách
            }

            // Výpis informací o spuštění pingování
            Console.WriteLine($"Pingování {target} s timeoutem {timeout}ms, {pingCount} pokusů...\n");

            try
            {
                // Spuštění hlavní pingovací funkce
                await PingHost(target, timeout, pingCount);
            }
            catch (Exception ex)  // Zachycení neočekávaných chyb
            {
                Console.WriteLine($"Došlo k neočekávané chybě: {ex.Message}");
            }

            // Console.WriteLine("\nStiskněte libovolnou klávesu pro ukončení...");
            // Console.ReadKey();
        }

        // Hlavní metoda pro pingování cílové adresy
        private static async Task PingHost(string target, int timeout, int pingCount)
        {
            using (Ping ping = new Ping())  // Vytvoření instance Ping s automatickým uvolněním prostředků
            {
                // Konfigurace ping options - nastavení fragmentace paketů
                PingOptions options = new PingOptions { DontFragment = true };
                // Buffer s daty k odeslání (32 bajtů)
                byte[] buffer = new byte[32];
                int successfulPings = 0;  // Počítadlo úspěšných pingů
                long totalTime = 0;       // Celkový čas pro výpočet průměru

                // Smyčka pro provedení zadaného počtu pingů
                for (int i = 0; i < pingCount; i++)
                {
                    try
                    {
                        // Asynchronní odeslání ping požadavku
                        PingReply reply = await ping.SendPingAsync(target, timeout, buffer, options);

                        // Zpracování odpovědi
                        ProcessPingReply(reply, i + 1);

                        // Pokud byl ping úspěšný, aktualizovat statistiky
                        if (reply.Status == IPStatus.Success)
                        {
                            successfulPings++;
                            totalTime += reply.RoundtripTime;
                        }

                        // Čekání 1 sekundu mezi pingy (kromě posledního)
                        if (i < pingCount - 1)
                        {
                            await Task.Delay(1000);
                        }
                    }
                    catch (PingException ex)  // Specifická chyba pingování
                    {
                        Console.WriteLine($"{i + 1}. Pokus - Chyba pingování: {ex.Message}");
                    }
                    catch (Exception ex)  // Obecná neočekávaná chyba
                    {
                        Console.WriteLine($"{i + 1}. Pokus - Neočekávaná chyba: {ex.Message}");
                    }
                }

                // Výpis statistik po dokončení všech pingů
                if (successfulPings > 0)
                {
                    Console.WriteLine($"\nStatistika:");
                    Console.WriteLine($"  Úspěšné pingy: {successfulPings}/{pingCount}");
                    Console.WriteLine($"  Průměrný čas: {totalTime / successfulPings}ms");
                }
            }
        }

        // Metoda pro zpracování a zobrazení výsledku jednotlivého ping požadavku
        private static void ProcessPingReply(PingReply reply, int attempt)
        {
            Console.Write($"{attempt}. ");  // Číslo pokusu

            if (reply.Status == IPStatus.Success)  // Pokud byl ping úspěšný
            {
                // Výpis detailních informací o odpovědi
                Console.WriteLine($"Odpověď od {reply.Address}: " +
                    $"bytes={reply.Buffer.Length} " +        // Velikost přijatých dat
                    $"time={reply.RoundtripTime}ms " +      // Doba odezvy
                    $"TTL={reply.Options?.Ttl ?? 0}");      // Time To Live (s kontrola na null)
            }
            else  // Pokud ping selhal
            {
                Console.Write($"Chyba: {GetStatusDescription(reply.Status)}");  // Popis chyby
                if (reply.Address != null)  // Pokud máme adresu, i když došlo k chybě
                    Console.Write($" from {reply.Address}");
                Console.WriteLine();
            }
        }

        // Pomocná metoda pro převod stavu IPStatus na čitelný popis v češtině
        private static string GetStatusDescription(IPStatus status)
        {
            return status switch  // Přepínač pro různé stavy
            {
                IPStatus.TimedOut => "Vypršel časový limit",
                IPStatus.DestinationHostUnreachable => "Cílový uzel není dostupný",
                IPStatus.DestinationNetworkUnreachable => "Cílová síť není dostupná",
                IPStatus.DestinationProtocolUnreachable => "Cílový protokol není dostupný",
                IPStatus.DestinationPortUnreachable => "Cílový port není dostupný",
                _ => status.ToString()  // Výchozí případ - vrátí textovou reprezentaci enumu
            };
        }
    }
}

/*
Zpracování argumentů - Přijímá cílovou adresu, volitelný timeout a počet pingů
Chybová kontrola - Validuje vstupy a ošetřuje výjimky
Asynchronní operace - Používá async/await pro nemařící operace
Konfigurovatelné parametry - Velikost bufferu, fragmentace paketů
Podrobné výstupy - Zobrazuje TTL, velikost dat, dobu odezvy
Statistiky - Poskytuje přehled úspěšnosti a průměrnou dobu odezvy
*/