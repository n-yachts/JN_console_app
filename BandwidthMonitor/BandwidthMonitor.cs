using System;  // Základní jmenný prostor pro vstup/výstup, základní třídy
using System.Collections.Generic;  // Práce s kolekcemi (List, Dictionary atd.)
using System.Diagnostics;  // Práce s procesy a stopkami
using System.Net.NetworkInformation;  // Síťové informace a monitorování
using System.Threading;  // Práce s vlákny a asynchronními operacemi
using System.Threading.Tasks;  // Asynchronní programování

class BandwidthMonitor  // Hlavní třída pro monitorování šířky pásma
{
    // Seznam pro sledování všech běžících monitorovacích úloh
    private static readonly List<Task> _monitoringTasks = new List<Task>();
    // Zdroj tokenu pro zrušení všech monitorovacích úloh
    private static CancellationTokenSource _cancellationTokenSource;

    // Hlavní asynchronní vstupní bod aplikace
    static async Task Main(string[] args)
    {
        // Výstup úvodních informací
        Console.WriteLine("Síťový Bandwidth Monitor");
        Console.WriteLine("Stiskni Enter nebo Ctrl+C pro zastavení\n");

        // Inicializace zdroje tokenu pro zrušení
        _cancellationTokenSource = new CancellationTokenSource();

        // Registrace obsluhy události pro Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;  // Zabránit standardnímu ukončení
            Console.WriteLine("\nUkončování monitorování...");
            _cancellationTokenSource.Cancel();  // Signalizace všem úlohám k ukončení
        };

        try
        {
            // Získání seznamu aktivních síťových rozhraní
            var interfaces = GetActiveInterfaces();

            // Kontrola existence aktivních rozhraní
            if (interfaces.Count == 0)
            {
                Console.WriteLine("Nenalezena žádná aktivní síťová rozhraní");
                return;  // Ukončení aplikace pokud nejsou rozhraní
            }

            // Výpis počtu nalezených rozhraní
            Console.WriteLine($"Nalezeno aktivních rozhraní: {interfaces.Count}\n");

            // Spuštění monitorování pro každé nalezené rozhraní
            foreach (var ni in interfaces)
            {
                StartMonitoring(ni);
            }

            // Čekání na dokončení všech monitorovacích úloh
            await Task.WhenAll(_monitoringTasks);
        }
        catch (OperationCanceledException)  // Ošetření řízeného zrušení
        {
            Console.WriteLine("Monitorování bylo ukončeno.");
        }
        catch (Exception ex)  // Ošetření neočekávaných chyb
        {
            Console.WriteLine($"Došlo k chybě: {ex.Message}");
        }
    }

    // Metoda pro získání aktivních síťových rozhraní
    private static List<NetworkInterface> GetActiveInterfaces()
    {
        // Inicializace seznamu pro aktivní rozhraní
        var activeInterfaces = new List<NetworkInterface>();
        // Získání všech dostupných síťových rozhraní
        var allInterfaces = NetworkInterface.GetAllNetworkInterfaces();

        // Procházení všech rozhraní
        foreach (var ni in allInterfaces)
        {
            // Filtrování - pouze rozhraní která jsou:
            // - Aktivní (Up)
            // - Nejsou loopback
            // - Podporují IPv4
            if (ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.Supports(NetworkInterfaceComponent.IPv4))
            {
                activeInterfaces.Add(ni);  // Přidání vyhovujícího rozhraní
            }
        }

        return activeInterfaces;  // Návrat filtrovaného seznamu
    }

    // Spuštění monitorování konkrétního síťového rozhraní
    private static void StartMonitoring(NetworkInterface ni)
    {
        // Vytvoření nové úlohy pro monitorování
        var task = Task.Run(async () =>
        {
            try
            {
                // Spuštění hlavní monitorovací smyčky
                await MonitorInterface(ni, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Očekávaná výjimka při zrušení - není potřeba řešit
            }
            catch (Exception ex)  // Zachycení neočekávaných chyb
            {
                Console.WriteLine($"Chyba u rozhraní {ni.Name}: {ex.Message}");
            }
        });

        // Přidání úlohy do seznamu pro sledování
        _monitoringTasks.Add(task);
    }

    // Hlavní metoda pro monitorování síťového rozhraní
    static async Task MonitorInterface(NetworkInterface ni, CancellationToken cancellationToken)
    {
        // Spuštění stopek pro měření času
        var stopwatch = Stopwatch.StartNew();
        // Získání počátečních statistik rozhraní
        var initialStats = ni.GetIPv4Statistics();
        // Uložení počátečních hodnot přenesených dat
        long lastBytesReceived = initialStats.BytesReceived;
        long lastBytesSent = initialStats.BytesSent;

        // Výpis informací o spuštěném monitorování
        Console.WriteLine($"Spouštím monitorování: {ni.Name} ({ni.Description})");

        // Hlavní monitorovací smyčka - běží dokud není požadováno zrušení
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Čekání 1 sekundu (s možností přerušení)
                await Task.Delay(1000, cancellationToken);

                // Získání aktuálních statistik
                var currentStats = ni.GetIPv4Statistics();
                long currentBytesReceived = currentStats.BytesReceived;
                long currentBytesSent = currentStats.BytesSent;

                // Výpočet rozdílu oproti předchozímu měření
                long receivedDelta = currentBytesReceived - lastBytesReceived;
                long sentDelta = currentBytesSent - lastBytesSent;

                // Výpočet aktuální rychlosti přenosu
                (double receivedSpeed, string receivedUnit) = CalculateSpeed(receivedDelta);
                (double sentSpeed, string sentUnit) = CalculateSpeed(sentDelta);

                // Formátování celkových přenesených dat
                string totalReceived = FormatBytes(currentBytesReceived);
                string totalSent = FormatBytes(currentBytesSent);

                // Výpis výsledků pro aktuální rozhraní
                Console.WriteLine(
                    $"[{ni.Name}] " +
                    $"↓ {receivedSpeed:0.00} {receivedUnit}/s | " +  // Download speed
                    $"↑ {sentSpeed:0.00} {sentUnit}/s | " +          // Upload speed
                    $"Celkem: ↓ {totalReceived} ↑ {totalSent}");     // Total data

                // Aktualizace předchozích hodnot pro příští iteraci
                lastBytesReceived = currentBytesReceived;
                lastBytesSent = currentBytesSent;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Zachycení chyb během monitorování (kromě zrušení)
                Console.WriteLine($"Chyba při monitorování {ni.Name}: {ex.Message}");
                break;  // Ukončení monitorování tohoto rozhraní
            }
        }
    }

    // Metoda pro výpočet rychlosti přenosu v odpovídajících jednotkách
    private static (double speed, string unit) CalculateSpeed(long bytes)
    {
        double bits = bytes * 8;  // Převod bytů na bity

        // Pole jednotek pro postupný převod
        string[] units = { "kbps", "Mbps", "Gbps" };
        int unitIndex = 0;  // Začínáme na kilobitech za sekundu
        double speed = bits / 1000.0;  // Základní převod na kbps

        // Postupný převod na vyšší jednotky dokud to má smysl
        while (speed >= 1000.0 && unitIndex < units.Length - 1)
        {
            speed /= 1000.0;  // Převod na vyšší jednotku
            unitIndex++;      // Posun v poli jednotek
        }

        return (speed, units[unitIndex]);  // Návrat rychlosti a jednotky
    }

    // Metoda pro formátování velikosti dat do čitelné podoby
    static string FormatBytes(long bytes)
    {
        // Pole přípon pro jednotky velikosti
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;  // Počáteční index jednotky
        double number = bytes;  // Vstupní hodnota

        // Postupný převod na vyšší jednotky
        while (number >= 1024.0 && counter < suffixes.Length - 1)
        {
            number /= 1024.0;  // Dělení binární soustavou
            counter++;         // Posun na další jednotku
        }

        // Návrat formátované hodnoty s příponou
        return $"{number:0.0} {suffixes[counter]}";
    }
}

/*
Inicializace a řízení životního cyklu
 Main - Hlavní vstupní bod aplikace
 CancellationTokenSource - Řízení graceful shutdown
 Console.CancelKeyPress - Obsluha Ctrl+C
Detekce síťových rozhraní
 GetActiveInterfaces() - Filtruje pouze aktivní IPv4 rozhraní
 Kontrola OperationalStatus.Up a NetworkInterfaceType
Paralelní monitorování
 StartMonitoring() - Spouští samostatné úlohy pro každé rozhraní
 Task.WhenAll() - Synchronizace všech monitorovacích úloh
Výpočetní algoritmy
 CalculateSpeed() - Převod bytů na síťové jednotky (kbps/Mbps/Gbps)
 FormatBytes() - Formátování pro čitelný výstup (KB/MB/GB/TB)
 Měření rozdílů přenesených dat v 1-sekundových intervalech
Error handling
 Zachycení OperationCanceledException pro čisté ukončení
 Samostatné zachycení chyb pro každé rozhraní
 Hlášení chyb bez pádu celé aplikace

Aplikace poskytuje real-time monitoring síťového vytížení s automatickou detekcí jednotek a podporou pro všechna aktivní síťová rozhraní.
*/