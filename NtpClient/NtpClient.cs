using System;           // Základní jmenný prostor pro práci s datovými typy, výjimkami atd.
using System.Net;       // Poskytuje třídy pro práci se sítí (DNS, IP adresy)
using System.Net.Sockets; // Obsahuje implementaci socketů pro síťovou komunikaci

class NtpClient         // Hlavní třída NTP klienta
{
    static void Main(string[] args)  // Hlavní vstupní bod programu
    {
        // Pole veřejných NTP serverů pro dotazování
        string[] ntpServers = {
            "pool.ntp.org",     // Veřejný pool NTP serverů
            "time.google.com",   // Google Time Server
            "time.windows.com",  // Microsoft Time Server
            "time.nist.gov",      // Národní institut standardů a technologií (USA)
            "tik.cesnet.cz",      // Český NTP server provozovaný CESNETem
            "tak.cesnet.cz"      // Další český NTP server CESNETu
        };

        Console.WriteLine("NTP Time Synchronizer\n");  // Výpis hlavičky programu

        // Cyklus procházející všechny NTP servery v poli
        foreach (string server in ntpServers)
        {
            try
            {
                // Získání času z NTP serveru
                DateTime ntpTime = GetNetworkTime(server);
                // Získání lokálního systémového času
                DateTime localTime = DateTime.Now;
                // Výpočet rozdílu mezi NTP a lokálním časem
                TimeSpan difference = ntpTime - localTime;

                // Výpis informací pro aktuální server
                Console.WriteLine($"Server: {server}");
                // Formátovaný výpis času s milisekundami
                Console.WriteLine($"NTP Time:    {ntpTime:yyyy-MM-dd HH:mm:ss.fff}");
                Console.WriteLine($"Local Time:  {localTime:yyyy-MM-dd HH:mm:ss.fff}");
                // Výpis rozdílu s vlastním formátováním znaménka
                Console.WriteLine($"Difference:  {difference.TotalMilliseconds:+0.##;-0.##} ms");
                Console.WriteLine();  // Prázdný řádek pro lepší čitelnost
            }
            catch (Exception ex)  // Zachycení výjimek pro konkrétní server
            {
                // Výpis chybové zprávy bez ukončení programu
                Console.WriteLine($"Chyba u {server}: {ex.Message}\n");
            }
        }
    }

    static DateTime GetNetworkTime(string ntpServer)  // Metoda pro získání času z NTP
    {
        // Vytvoření NTP packetu - 48 bytů podle specifikace protokolu
        var ntpData = new byte[48];
        // Nastavení hlavičky packetu: 
        // 0x1B = 00 011 011 (LI=0, VN=3, Mode=3)
        // LI=0 - žádné varování, VN=3 - verze 3, Mode=3 - klient
        ntpData[0] = 0x1B;

        // Překlad doménového jména na IP adresu
        var addresses = Dns.GetHostEntry(ntpServer).AddressList;
        // Vytvoření koncového bodu pro spojení (první resolved adresa, port 123)
        var ipEndPoint = new IPEndPoint(addresses[0], 123);

        // Vytvoření UDP socketu pomocí using pro automatické uvolnění prostředků
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Connect(ipEndPoint);  // Připojení k NTP serveru
            socket.Send(ntpData);        // Odeslání NTP dotazu
            socket.Receive(ntpData);     // Příjem odpovědi (přepíše původní ntpData)
        }

        // Pozice timestampu v odpovědi (64 bitů - 8 bytů od pozice 40)
        const byte serverReplyTime = 40;
        // Převod prvních 4 bytů na unsigned integer (celočíselná část timestampu)
        ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
        // Převod následujících 4 bytů (desetinná část timestampu)
        ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

        // Převod z network byte order (big-endian) na hostitelský formát
        intPart = SwapEndianness(intPart);
        fractPart = SwapEndianness(fractPart);

        // Výpočet milisekund od 1.1.1900
        // Celá část * 1000 + (desetinná část * 1000) / 2^32
        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        // Vytvoření data počítajícího od 1.1.1900 v UTC a přidání vypočtených ms
        var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMilliseconds((long)milliseconds);

        return networkDateTime.ToLocalTime();  // Převod UTC času na lokální časovou zónu
    }

    // Metoda pro převod endianity (změna pořadí bytů)
    static uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) +  // Posun 1. bytu na pozici 4
                       ((x & 0x0000ff00) << 8) +   // Posun 2. bytu na pozici 3
                       ((x & 0x00ff0000) >> 8) +   // Posun 3. bytu na pozici 2
                       ((x & 0xff000000) >> 24));  // Posun 4. bytu na pozici 1
    }
}

/*
NTP Protocol:
 Používá UDP port 123
 Čas se počítá od 1.1.1900
 Časová značka je 64 bitů (32 bitů celá část, 32 bitů desetinná)
Endianita:
 NTP používá big-endian formát
 Moderní počítače často používají little-endian
 Metoda SwapEndianness zajišťuje správnou interpretaci bytů
Zpracování času:
 Převod z UTC na lokální čas zohledňuje časové pásmo
 Rozlišení na milisekundy umožňuje přesné porovnání
Ošetření chyb:
 Program pokračuje při chybě jednoho serveru
 Zachycují se síťové chyby i chyby DNS

Tento kód slouží jako ukázka principů NTP komunikace a není vhodný pro přesnou časovou synchronizaci v produkčním prostředí.
*/