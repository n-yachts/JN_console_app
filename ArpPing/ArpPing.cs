using System;  // Základní jmenný prostor pro základní třídy jako Console, Exception
using System.ComponentModel;  // Pro třídu Win32Exception pro práci s systémovými chybami
using System.Net;  // Pro práci s IP adresami (IPAddress)
using System.Net.NetworkInformation;  // Pro kontrolu vlastností IP adres (multicast/broadcast)
using System.Runtime.InteropServices;  // Pro práci s nativním kódem pomocí DllImport
using System.Text;  // Práce s textovými řetězci (v tomto kódu se přímo nepoužívá)

namespace ArpPing  // Definice jmenného prostoru pro organizaci kódu
{
    internal class ArpPing  // Hlavní třída programu
    {
        // Import nativní funkce SendARP z knihovny iphlpapi.dll
        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(
            int destIp,          // Cílová IP adresa v podobě integeru
            int srcIp,           // Zdrojová IP (0 pro výchozí)
            byte[] macAddr,      // Výstupní buffer pro MAC adresu
            ref uint physicalAddrLen);  // Délka bufferu pro MAC adresu

        static void Main(string[] args)  // Hlavní vstupní bod programu
        {
            // Kontrola počtu argumentů - program vyžaduje přesně 1 argument
            if (args.Length != 1)
            {
                Console.WriteLine("Použití: ArPing <IP adresa>");
                return;  // Ukončení programu při chybném počtu argumentů
            }

            try  // Zachycení výjimek během parsování IP adresy a volání ARP
            {
                // Převedení vstupního řetězce na objekt IPAddress
                IPAddress target = IPAddress.Parse(args[0]);

                // Kontrola jestli se nejedná o multicast/broadcast
                // ARP protokol nelze použít pro tyto typy adres
                if (target.IsIPv6Multicast || IPAddress.Broadcast.Equals(target))
                {
                    Console.WriteLine("Chyba: ARP nelze použít pro multicast/broadcast adresy");
                    return;
                }

                // Inicializace pole pro MAC adresu (6 bajtů pro Ethernet)
                byte[] macAddr = new byte[6];
                uint macLen = (uint)macAddr.Length;  // Převod délky na unsigned integer

                // Volání nativní funkce SendARP
                int result = SendARP(
                    BitConverter.ToInt32(target.GetAddressBytes(), 0),  // Převod IP na int
                    0,  // Nulová zdrojová IP = použije se výchozí rozhraní
                    macAddr,  // Buffer pro přijetí MAC adresy
                    ref macLen);  // Reference na délku bufferu

                // Vyhodnocení výsledku volání ARP
                if (result == 0 && macLen > 0)  // Úspěch: návratový kód 0 a platná délka MAC
                {
                    // Výpis úspěšného výsledku s naformátovanou MAC adresou
                    Console.WriteLine($"IP: {target} -> MAC: {FormatMacAddress(macAddr)}");
                }
                else  // Neúspěšné vyhledání MAC adresy
                {
                    Console.WriteLine($"Zařízení {target} nebylo nalezeno");
                    // Podrobnější informace o chybě z Win32 API
                    if (result != 0)
                    {
                        Console.WriteLine($"Chyba ARP: {new Win32Exception(result).Message} (kód: {result})");
                    }
                }
            }
            catch (FormatException)  // Specifická výjimka pro chybný formát IP adresy
            {
                Console.WriteLine("Chyba: Neplatný formát IP adresy");
            }
            catch (Exception ex)  // Obecná zachycení všech ostatních výjimek
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
        }

        // Pomocná metoda pro formátování MAC adresy do lidsky čitelné podoby
        private static string FormatMacAddress(byte[] mac)
        {
            // Převede byte pole na řetězec s oddělovači a převede na malá písmena
            return BitConverter.ToString(mac).ToLower().Replace('-', ':');
        }
    }
}

/*
DllImport pro SendARP:
 Připojuje se k Windows API funkci pro odesílání ARP požadavků
 ExactSpelling = true zajišťuje přesné hledání funkce v DLL
Převod IP adresy:
 BitConverter.ToInt32() převádí 4-bajtovou IP adresu na integer
 GetAddressBytes() získá bajtové vyjádření IP adresy
Práce s MAC adresou:
 Fixed-size pole 6 bajtů pro Ethernetové MAC adresy
 macLen slouží jako vstupní/výstupní parametr pro délku MAC adresy
Chybové stavy:
 Návratová hodnota 0 = úspěch
 Jiné hodnoty = chyba, interpretovaná pomocí Win32Exception
 Délka MAC adresy 0 = zařízení nebylo nalezeno
Formátování výstupu:
 Metoda FormatMacAddress vytváří standardní formát MAC adresy (např. 01:23:45:ab:cd:ef)
*/