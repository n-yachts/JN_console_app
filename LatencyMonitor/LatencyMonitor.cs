using System;  // Import základních systémových funkcí a tříd (např. Console, DateTime)
using System.Diagnostics;  // Import tříd pro diagnostiku (např. Stopwatch)
using System.Net.NetworkInformation;  // Import síťových funkcí (např. Ping, PingReply)
using System.Threading.Tasks;  // Import pro asynchronní programování (Task, async/await)

class LatencyMonitor  // Hlavní třída programu pro monitorování latence
{
    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
    {
        // Kontrola, zda byly zadány argumenty (seznam hostitelů)
        if (args.Length == 0)
        {
            Console.WriteLine("Použití: LatencyMonitor <host1> <host2> ...");
            return;  // Ukončení programu pokud nejsou zadány žádné argumenty
        }

        Console.WriteLine("Monitoring latence (Ctrl+C pro ukončení)\n");

        // Nekonečná smyčka pro průběžné monitorování
        while (true)
        {
            // Cyklus přes všechny zadané hostitele z argumentů příkazové řádky
            foreach (string host in args)
            {
                try
                {
                    // Asynchronní měření latence pro aktuálního hostitele
                    long latency = await MeasureLatency(host);
                    // Výpis času, hostitele a naměřené latence
                    Console.WriteLine($"{DateTime.Now:T} {host}: {latency}ms");
                }
                catch  // Zachycení jakékoliv chyby během měření
                {
                    // Výpis chybového hlášení při selhání měření
                    Console.WriteLine($"{DateTime.Now:T} {host}: TIMEOUT");
                }
            }

            Console.WriteLine("---");  // Oddělovací čára mezi cykly měření
            await Task.Delay(5000);  // Čekání 5 sekund před dalším měřením
        }
    }

    // Asynchronní metoda pro měření latence pomocí ICMP ping
    static async Task<long> MeasureLatency(string host)
    {
        Ping ping = new Ping();  // Vytvoření instance třídy Ping
        Stopwatch sw = Stopwatch.StartNew();  // Spuštění stopek pro přesnější měření

        // Odeslání asynchronního ping požadavku s timeoutem 1000ms (1 sekunda)
        PingReply reply = await ping.SendPingAsync(host, 1000);
        sw.Stop();  // Zastavení stopek

        // Kontrola, zda byl ping úspěšný
        if (reply.Status != IPStatus.Success)
            throw new Exception("Ping failed");  // Vyhození výjimky při neúspěchu

        // Vrácení naměřené doby odezvy z ping reply
        return reply.RoundtripTime;
    }
}

/*
Inicializace programu:
 Kontroluje se přítomnost argumentů příkazové řádky (seznam hostitelů)
 Program se ukončí, pokud nejsou zadány žádné argumenty
Hlavní monitorovací smyčka:
 Nekonečný cyklus průběžného měření
 Pro každého hostitele:
  Měří latenci asynchronně
  Výpis úspěšného měření nebo oznámení o timeoutu
 Mezi cykly je 5vteřinová pauza
Metoda MeasureLatency:
 Používá třídu Ping pro odesílání ICMP Echo requestů
 Kombinuje systémový ping s stopkami pro přesné měření
 Timeout nastaven na 1 sekundu
 Vrací hodnotu RTT (Round-Trip Time) nebo vyhazuje výjimku
*/