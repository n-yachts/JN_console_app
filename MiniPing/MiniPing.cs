using System;  // Import základních systémových knihoven
using System.Net.NetworkInformation;  // Import knihoven pro síťové funkce (Ping)
using System.Threading;  // Import knihoven pro vlákna a časování

namespace MiniPing  // Deklarace jmenného prostoru pro organizaci kódu
{
    internal class MiniPing  // Hlavní třída programu
    {
        static void Main(string[] args)  // Hlavní vstupní bod aplikace
        {
            // Kontrola zda byly zadány argumenty
            if (args.Length == 0)
            {
                // Výpis nápovědy pokud nebyly zadány argumenty
                Console.WriteLine("Použití: MiniPing <adresa> [timeout v ms]");
                Console.WriteLine("Příklad: MiniPing google.com");
                Console.WriteLine("Příklad: MiniPing 192.168.1.1 100");
                return;  // Ukončení programu
            }

            string target = args[0];  // Uložení prvního argumentu jako cílové adresy
            int timeout = 1000;  // Výchozí hodnota timeoutu 1000ms (1s)

            // Zpracování druhého argumentu (timeout) pokud byl zadán
            if (args.Length > 1)
            {
                // Pokus o převod druhého argumentu na číslo
                if (!int.TryParse(args[1], out timeout))
                {
                    Console.WriteLine("Chyba: Timeout musí být číslo!");
                    return;  // Ukončení programu při chybě
                }

                // Kontrola platnosti timeoutu
                if (timeout < 1)
                {
                    Console.WriteLine("Chyba: Timeout musí být alespoň 1ms!");
                    return;  // Ukončení programu při chybě
                }
            }

            // Výpis informací o probíhající operaci
            Console.WriteLine($"Pingování {target} s timeoutem {timeout}ms...");
            Console.WriteLine();  // Prázdný řádek

            try  // Zachycení výjimek během pingování
            {
                using (Ping ping = new Ping())  // Vytvoření ping objektu s automatickým uklizením
                {
                    PingOptions options = new PingOptions();  // Vytvoření možností pro ping
                    options.DontFragment = true;  // Zapnutí příznaku nefragmentování paketů

                    byte[] buffer = new byte[32];  // Vytvoření 32bajtového bufferu s daty

                    // Odeslání ping požadavku s parametry:
                    // - cílová adresa
                    // - timeout
                    // - data (buffer)
                    // - možnosti
                    PingReply reply = ping.Send(target, timeout, buffer, options);

                    // Zpracování odpovědi voláním pomocné metody
                    ZpracujOdpoved(reply);
                }
            }
            catch (PingException ex)  // Specifická výjimka pro chyby pingování
            {
                Console.WriteLine($"Chyba pingování: {ex.Message}");
            }
            catch (Exception ex)  // Obecná výjimka pro neočekávané chyby
            {
                Console.WriteLine($"Neočekávaná chyba: {ex.Message}");
            }

            // Čekání na uživatelský vstup před ukončením
            Console.WriteLine("\nStiskněte libovolnou klávesu pro ukončení...");
            Console.ReadKey();  // Čtení stisknuté klávesy
        }

        // Pomocná metoda pro zpracování ping odpovědi
        private static void ZpracujOdpoved(PingReply reply)
        {
            // Kontrola zda byla obdržena platná odpověď
            if (reply == null)
            {
                Console.WriteLine("Chyba: Obdržena null odpověď");
                return;  // Předčasné ukončení metody
            }

            // Výpis základních informací o odpovědi
            Console.WriteLine($"Odpověď od {reply.Address}:");
            Console.WriteLine($"  Stav: {reply.Status}");

            // Podrobnější analýza při úspěšné odpovědi
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine($"  Čas: {reply.RoundtripTime}ms");  // Doba odezvy
                Console.WriteLine($"  Délka: {reply.Buffer.Length} bajtů");  // Velikost odpovědi
                Console.WriteLine($"  TTL: {reply.Options?.Ttl ?? 0}");  // Time To Live (s kontrola null)
            }
            else  // Zpracování různých typů chyb
            {
                switch (reply.Status)
                {
                    case IPStatus.TimedOut:
                        Console.WriteLine("  Požadavek vypršel - cílový uzel neodpověděl v stanoveném čase.");
                        break;
                    case IPStatus.DestinationHostUnreachable:
                        Console.WriteLine("  Cílový uzel není dostupný.");
                        break;
                    case IPStatus.DestinationNetworkUnreachable:
                        Console.WriteLine("  Cílová síť není dostupná.");
                        break;
                    default:
                        Console.WriteLine($"  Detaily: {reply.Status}");  // Výchozí výpis stavu
                        break;
                }
            }
        }
    }
}

/*
Namespace a using direktivy - Organizace kódu a import potřebných knihoven
Zpracování argumentů - Načítání a validace vstupních parametrů
Konfigurace ping - Nastavení timeoutu a voleb pro odesílání paketů
Odeslání ping požadavku - Vlastní síťová operace pomocí třídy Ping
Zpracování odpovědi - Analýza výsledku a výpis informací
Ošetření chyb - Zachycení a zpracování výjimek
Uživatelský výstup - Podrobná informace o výsledku operace

Program funguje jako jednoduchý nástroj pro testování dostupnosti síťových zařízení, podobný systémovému příkazu ping
*/