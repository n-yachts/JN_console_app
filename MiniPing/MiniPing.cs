using System;
using System.Net.NetworkInformation;
using System.Threading;

namespace MiniPing
{
    internal class MiniPing
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Použití: MiniPing <adresa> [timeout v ms]");
                Console.WriteLine("Příklad: MiniPing google.com");
                Console.WriteLine("Příklad: MiniPing 192.168.1.1 100");
                return;
            }

            string target = args[0];
            int timeout = 1000; // Výchozí timeout 1s

            // Zpracování timeoutu z argumentů
            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out timeout))
                {
                    Console.WriteLine("Chyba: Timeout musí být číslo!");
                    return;
                }

                if (timeout < 1)
                {
                    Console.WriteLine("Chyba: Timeout musí být alespoň 1ms!");
                    return;
                }
            }

            Console.WriteLine($"Pingování {target} s timeoutem {timeout}ms...");
            Console.WriteLine();

            try
            {
                using (Ping ping = new Ping())
                {
                    PingOptions options = new PingOptions();
                    options.DontFragment = true;

                    byte[] buffer = new byte[32];

                    // Odeslání ping požadavku
                    PingReply reply = ping.Send(target, timeout, buffer, options);

                    ZpracujOdpoved(reply);
                }
            }
            catch (PingException ex)
            {
                Console.WriteLine($"Chyba pingování: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Neočekávaná chyba: {ex.Message}");
            }

            Console.WriteLine("\nStiskněte libovolnou klávesu pro ukončení...");
            Console.ReadKey();
        }

        private static void ZpracujOdpoved(PingReply reply)
        {
            if (reply == null)
            {
                Console.WriteLine("Chyba: Obdržena null odpověď");
                return;
            }

            Console.WriteLine($"Odpověď od {reply.Address}:");
            Console.WriteLine($"  Stav: {reply.Status}");

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine($"  Čas: {reply.RoundtripTime}ms");
                Console.WriteLine($"  Délka: {reply.Buffer.Length} bajtů");
                Console.WriteLine($"  TTL: {reply.Options?.Ttl ?? 0}");
            }
            else
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
                        Console.WriteLine($"  Detaily: {reply.Status}");
                        break;
                }
            }
        }
    }
}