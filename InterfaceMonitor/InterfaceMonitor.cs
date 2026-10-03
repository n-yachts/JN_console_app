using System;  // Importování základních systémových knihoven
using System.Net.NetworkInformation;  // Importování knihovny pro práci se síťovými rozhraními
using System.Threading;  // Importování knihovny pro vlákna a asynchronní operace
using System.Threading.Tasks;  // Importování knihovny pro úlohy (Task)

class InterfaceMonitor  // Deklarace třídy monitorující síťová rozhraní
{
    static async Task Main()  // Hlavní asynchronní metoda programu
    {
        Console.WriteLine("Monitor síťových rozhraní (Ctrl+C pro ukončení)\n");  // Výpis úvodní zprávy

        using var cancellationToken = new CancellationTokenSource();  // Vytvoření zdroje tokenu pro zrušení operace
        Console.CancelKeyPress += (s, e) => {  // Registrace obsluhy události stisku Ctrl+C
            e.Cancel = true;  // Zabránění standardnímu ukončení procesu
            cancellationToken.Cancel();  // Aktivace žádosti o zrušení
        };

        while (!cancellationToken.Token.IsCancellationRequested)  // Hlavní smyčka dokud není požadováno zrušení
        {
            Console.Clear();  // Vymazání konzole pro čistší zobrazení
            DisplayInterfaceInfo();  // Volání metody pro zobrazení informací o rozhraních
            try
            {
                await Task.Delay(2000, cancellationToken.Token);  // Čekání 2 sekundy s možností zrušení
            }
            catch (TaskCanceledException)
            {
                // Ctrl+C přeruší čekání - smyčka tím korektně skončí
            }
        }
    }

    static void DisplayInterfaceInfo()  // Metoda pro zobrazení informací o síťových rozhraních
    {
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();  // Získání všech síťových rozhraní

        foreach (NetworkInterface ni in interfaces)  // Cyklus přes všechna rozhraní
        {
            Console.WriteLine($"{ni.Name} ({ni.NetworkInterfaceType})");  // Výpis názvu a typu rozhraní
            Console.WriteLine($"  Status: {ni.OperationalStatus}");  // Výpis stavu rozhraní
            // Rychlost může být nedostupná (záporná hodnota)
            Console.WriteLine(ni.Speed > 0 ? $"  Speed: {ni.Speed / 1000000} Mbps" : "  Speed: N/A");

            // Rozhraní bez IPv4 (např. jen IPv6) nemá IPv4 statistiky
            if (!ni.Supports(NetworkInterfaceComponent.IPv4))
            {
                Console.WriteLine("  RX/TX: IPv4 není podporováno");
                Console.WriteLine();
                continue;
            }

            var stats = ni.GetIPv4Statistics();  // Získání IPv4 statistik rozhraní
            Console.WriteLine($"  RX: {FormatBytes(stats.BytesReceived)} | TX: {FormatBytes(stats.BytesSent)}");  // Výpis přijatých/odeslaných dat
            Console.WriteLine();  // Prázdný řádek pro oddělení
        }
    }

    static string FormatBytes(long bytes)  // Pomocná metoda pro formátování velikosti dat
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };  // Pole přípon pro jednotky
        int counter = 0;  // Počítadlo pro určení jednotky
        decimal number = bytes;  // Převedení vstupu na decimal pro přesný výpočet

        while (number >= 1024 && counter < suffixes.Length - 1)  // Cyklus pro převod na vyšší jednotky
        {
            number /= 1024;  // Dělení 1024 pro převod na vyšší jednotku
            counter++;  // Inkrementace počítadla jednotek
        }

        return $"{number:n1} {suffixes[counter]}";  // Vrácení naformátovaného řetězce
    }
}

/*
Inicializace a nastavení zrušení
 Program vytvoří CancellationTokenSource pro řízení ukončení
 Reaguje na stisk Ctrl+C elegantním ukončením smyčky (přerušené čekání Task.Delay vyvolá TaskCanceledException, která se zachytí)
Hlavní monitorovací smyčka
 Každé 2 sekundy obnovuje výpis informací
 Před každým obnovením vymaže konzoli pro přehlednost
Získání a zobrazení síťových informací
 Metoda GetAllNetworkInterfaces() vrací všechna dostupná síťová rozhraní
 Pro každé rozhraní zobrazuje:
  Název a typ
  Provozní stav
  Rychlost připojení
  Statistiku přijatých/odeslaných dat
Formátování velikostí
 Pomocná metoda převádí bajty na čitelné jednotky (B, KB, MB, GB, TB)
 Používá dělení 1024 a postupně zvyšuje jednotky
Program běží dokud uživatel nezmáčkne Ctrl+C, přičemž pravidelně aktualizuje síťové statistiky.
*/