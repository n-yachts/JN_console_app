using System;  // Základní jmenný prostor pro vstup/výstup a základní funkce
using System.Net;  // Jmenný prostor pro práci se sítí (IP adresy, síťové nástroje)
using System.Net.NetworkInformation;  // Obsahuje třídy pro síťové operace (Ping, PingOptions)
using System.Threading.Tasks;  // Podpora asynchronního programování

class TraceRoute  // Hlavní třída programu
{
    static async Task Main(string[] args)  // Asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - program vyžaduje přesně jeden argument
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: TraceRoute <hostname/IP>");
            return;  // Ukončení programu při chybném počtu argumentů
        }

        string target = args[0];  // Uložení cílové adresy z prvního argumentu
        int maxHops = 30;  // Maximální počet skoků (TTL) pro trasování
        int timeout = 1000;  // Časový limit pro odpověď v milisekundách

        // Výpis základních informací o trasování
        Console.WriteLine($"Trasování k {target} s maximálně {maxHops} skoky:\n");

        // Hlavní smyčka trasování - zvyšuje TTL pro každý skok
        for (int ttl = 1; ttl <= maxHops; ttl++)
        {
            // Vytvoření nového Ping objektu pro každý skok
            using (Ping ping = new Ping())
            {
                // Nastavení PingOptions - TTL a povolení fragmentace
                PingOptions options = new PingOptions(ttl, true);
                byte[] buffer = new byte[32];  // 32b buffer pro data ping požadavku

                // Asynchronní odeslání ping požadavku s danými parametry
                PingReply reply = await ping.SendPingAsync(target, timeout, buffer, options);

                // Zpracování úspěšné odpovědi (dosaženo cíle)
                if (reply.Status == IPStatus.Success)
                {
                    // Výpis čísla skoku, IP adresy a času odezvy
                    Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms");
                    Console.WriteLine("Trasování dokončeno.");
                    break;  // Ukončení smyčky při dosažení cíle
                }
                // Zpracování průběžné odpovědi (TTL vypršel na mezilehlém zařízení)
                else if (reply.Status == IPStatus.TtlExpired)
                {
                    Console.WriteLine($"{ttl}\t{reply.Address}\t{reply.RoundtripTime}ms");
                }
                // Žádná odpověď nebo chyba (timeout, nedostupný hostitel, atd.)
                else
                {
                    Console.WriteLine($"{ttl}\t*");
                }

                // Kontrola dosažení maximálního počtu skoků
                if (ttl == maxHops)
                {
                    Console.WriteLine("Dosaženo maximálního počtu skoků.");
                }
            }
        }
    }
}

/*
Princip trasování: Program využívá TTL (Time To Live) v IP paketech. Každý směrovač snižuje TTL o 1. Když TTL dosáhne 0, směrovač odešle chybové hlášení "TTL Expired".
PingOptions:
 TTL: Nastavuje maximální počet skoků pro aktuální pokus
 DontFragment: Zakazuje fragmentaci paketů
Stavy odpovědí:
 Success: Cíl dosažen
 TtlExpired: Paket dosáhl mezilehlého zařízení
 Ostatní: Timeout nebo chyba spojení
Průběh trasování:
 Začíná s TTL=1 (první směrovač)
 Postupně zvyšuje TTL až do dosažení cíle
 Každý skok ukazuje IP adresu mezilehlého zařízení

Program simuluje funkci systémového nástroje tracert/traceroute pomocí ICMP protokolu.
*/