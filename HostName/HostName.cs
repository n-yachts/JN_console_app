using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HostName
{
    class HostName
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Získání HostName z IP adresy ===\n");

            // Kontrola počtu argumentů
            if (args.Length != 1)
            {
                Console.WriteLine("Použití: HostName <IPv4 adresa>");
                return;  // Ukončení programu při chybném počtu argumentů
            }

            string ipAddress = args[0];  // Získání URL z prvního argumentu

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                IPAddress ip = IPAddress.Parse(ipAddress);

                var task = Dns.GetHostEntryAsync(ip);
                var completedTask = await Task.WhenAny(task, Task.Delay(5000, cts.Token));

                if (completedTask == task)
                {
                    IPHostEntry hostEntry = await task;
                    Console.WriteLine($"Název počítače: {hostEntry.HostName}");
                }
                else
                {
                    Console.WriteLine("Časový limit vypršel - server neodpovídá");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }
    }
}
