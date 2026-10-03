using System;  // Základní jmenný prostor pro Console, BitConverter, Math
using System.Collections.Generic;  // Dictionary pro tabulku názvů PGN
using System.IO.Ports;  // Práce se sériovým portem (SerialPort)

namespace Nmea2000Reader  // Jmenný prostor projektu
{
    // Zjednodušená čtečka NMEA 2000 (CAN bus) přes sériovou linku.
    // Vstupní formát je pro výukové účely zjednodušen: [PGN 3 B little-endian][zdrojová adresa 1 B][max. 8 B dat].
    // Skutečná zařízení (např. Actisense NGT-1, Yacht Devices) používají vlastní rámcování, proto je nutné jej doplnit.
    class Nmea2000Reader  // Hlavní třída programu
    {
        private static SerialPort _serialPort;  // Sériový port sdílený mezi metodami

        // Tabulka známých PGN (Parameter Group Number = číslo skupiny parametrů) a jejich názvů
        private static readonly Dictionary<uint, string> PgnNames = new Dictionary<uint, string>
        {
            { 126992, "System Time" },
            { 127245, "Rudder" },
            { 127250, "Vessel Heading" },
            { 127251, "Rate of Turn" },
            { 127257, "Attitude" },
            { 127258, "Magnetic Variation" },
            { 128259, "Speed" },
            { 128267, "Water Depth" },
            { 129025, "Position Rapid Update" },
            { 129026, "COG & SOG Rapid Update" },
            { 129029, "GNSS Position Data" },
            { 129033, "Time & Date" },
            { 129539, "GNSS DOPs" },
            { 129540, "GNSS Satellites in View" },
            { 130306, "Wind Data" },
            { 130310, "Environmental Parameters" },
            { 130311, "Temperature" },
            { 130312, "Pressure" },
            { 130313, "Humidity" },
            { 130314, "Actual Pressure" }
        };

        // Převodní konstanty (NMEA 2000 používá radiány a m/s)
        private const double RadToDeg = 180.0 / Math.PI;
        private const double MsToKnots = 1.0 / 0.514444;

        static void Main(string[] args)  // Hlavní vstupní bod programu
        {
            Console.WriteLine("NMEA 2000 Reader");
            Console.WriteLine("================\n");

            _serialPort = new SerialPort();

            // Výběr a nastavení portu uživatelem
            if (!ConfigureSerialPort())
            {
                Console.WriteLine("Nepodařilo se nakonfigurovat sériový port.");
                return;  // Ukončení programu při chybné konfiguraci
            }

            try  // Ošetření chyb při otevírání portu
            {
                _serialPort.Open();
                Console.WriteLine($"Připojeno k {_serialPort.PortName}, {_serialPort.BaudRate} baud");
                Console.WriteLine("Čtení NMEA 2000 dat... Stiskněte 'q' pro ukončení.\n");

                // Obsluha události se volá na samostatném vlákně vždy, když dorazí data
                _serialPort.DataReceived += SerialPort_DataReceived;

                // Hlavní smyčka - čeká na ukončení klávesou 'q' (zpracování dat probíhá v události)
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
            finally  // Uvolnění portu proběhne vždy
            {
                if (_serialPort?.IsOpen == true)
                    _serialPort.Close();
                _serialPort?.Dispose();
            }

            Console.WriteLine("\nProgram ukončen. Stiskněte libovolnou klávesu...");
            Console.ReadKey();
        }

        // Dotaz na uživatele a nastavení sériového portu; vrací false, pokud se nastavení nepodařilo
        static bool ConfigureSerialPort()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();  // Seznam portů dostupných v systému

                if (ports.Length == 0)
                {
                    Console.WriteLine("Nenalezeny žádné sériové porty!");
                    return false;
                }

                Console.WriteLine("Dostupné sériové porty:");
                for (int i = 0; i < ports.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {ports[i]}");
                }

                Console.Write("\nVyberte port (číslo nebo název): ");
                string input = Console.ReadLine();

                // Číslo ze seznamu vybere port podle pořadí, jinak se vstup bere jako přímý název portu
                if (int.TryParse(input, out int portNumber) && portNumber >= 1 && portNumber <= ports.Length)
                {
                    _serialPort.PortName = ports[portNumber - 1];
                }
                else
                {
                    _serialPort.PortName = input;
                }

                // Prázdný vstup = výchozí hodnota; neplatné číslo vyvolá výjimku zachycenou níže
                Console.Write("Baud rate (výchozí 115200): ");
                string baudInput = Console.ReadLine();
                _serialPort.BaudRate = string.IsNullOrEmpty(baudInput) ? 115200 : int.Parse(baudInput);

                // NMEA 2000 často používá vyšší baud rate
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Handshake = Handshake.None;
                _serialPort.ReadTimeout = 1000;
                _serialPort.WriteTimeout = 1000;
                _serialPort.ReadBufferSize = 4096;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba konfigurace: {ex.Message}");
                return false;
            }
        }

        // Obsluha události - zavolá se, když na sériový port dorazí data
        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;  // Počet bajtů čekajících v bufferu
                if (bytesToRead == 0) return;

                byte[] buffer = new byte[bytesToRead];
                _serialPort.Read(buffer, 0, bytesToRead);  // Přečtení všech dostupných bajtů

                ProcessNmea2000Data(buffer);  // Rozparsování zpráv
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení dat: {ex.Message}");
            }
        }

        // Zpracování přijatého bloku bajtů jako posloupnosti zpráv za sebou
        static void ProcessNmea2000Data(byte[] data)
        {
            // NMEA 2000 používá CAN bus frame formát (zde zjednodušený, viz komentář u třídy)
            // Zpracování jako stream dat - hledání kompletních zpráv
            for (int i = 0; i < data.Length; i++)
            {
                // Jednoduchá detekce začátku zprávy (může se lišit podle implementace)
                if (i + 8 <= data.Length) // Minimální délka pro nějakou užitečnou zprávu
                {
                    // Pokus o parsování jako NMEA 2000 zprávu
                    try
                    {
                        ProcessN2kMessage(data, ref i);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Chyba parsování zprávy: {ex.Message}");
                    }

                    // ProcessN2kMessage posune index za zprávu; for cyklus ho ještě zvýší, proto se vrací o 1 zpět
                    i--;
                }
            }
        }

        // Rozparsování jedné zprávy od pozice index; index se posune za zpracovanou zprávu
        static void ProcessN2kMessage(byte[] data, ref int index)
        {
            // ZÁKLADNÍ PARSOVÁNÍ NMEA 2000 ZPRÁVY
            // Toto je zjednodušená implementace - reálná implementace by byla komplexnější

            if (index + 3 >= data.Length) return;  // Příliš krátká zpráva

            // Předpokládáme, že data obsahují kompletní N2K zprávy

            // Získání PGN (Parameter Group Number) - 3 byty little-endian
            uint pgn = (uint)(data[index] | (data[index + 1] << 8) | (data[index + 2] << 16));
            index += 3;

            string pgnName = PgnNames.ContainsKey(pgn) ? PgnNames[pgn] : "Neznámý PGN";

            Console.WriteLine($"\n--- NMEA 2000 Zpráva ---");
            Console.WriteLine($"PGN: {pgn} ({pgnName})");
            Console.WriteLine($"Zdroj: {data[index++]:X2}");

            // Zbývající data
            int dataLength = Math.Min(8, data.Length - index); // Maximálně 8 bytů na CAN frame
            byte[] messageData = new byte[dataLength];
            Array.Copy(data, index, messageData, 0, dataLength);

            Console.WriteLine($"Data: {BitConverter.ToString(messageData).Replace("-", " ")}");

            // Specifické zpracování podle PGN
            ProcessPgnData(pgn, messageData);

            index += dataLength;
        }

        // Výběr zpracování podle čísla PGN
        static void ProcessPgnData(uint pgn, byte[] data)
        {
            try
            {
                switch (pgn)
                {
                    case 129025: // Position Rapid Update
                        ProcessPositionRapidUpdate(data);
                        break;
                    case 129026: // COG & SOG Rapid Update
                        ProcessCogSogRapidUpdate(data);
                        break;
                    case 129029: // GNSS Position Data - fast-packet (víc než 8 bajtů), jednoduchý parser ho nesloží
                        Console.WriteLine("(Fast-packet PGN - není podporováno)");
                        break;
                    case 129539: // GNSS DOPs
                        ProcessGnssDops(data);
                        break;
                    case 129033: // Time & Date
                        ProcessTimeDate(data);
                        break;
                    case 127250: // Vessel Heading
                        ProcessVesselHeading(data);
                        break;
                    case 128259: // Speed
                        ProcessSpeed(data);
                        break;
                    case 128267: // Water Depth
                        ProcessWaterDepth(data);
                        break;
                    case 130306: // Wind Data
                        ProcessWindData(data);
                        break;
                    default:
                        Console.WriteLine("(Nepodporované PGN pro detailní analýzu)");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba zpracování PGN {pgn}: {ex.Message}");
            }
        }

        // PGN 129025 - rychlá aktualizace polohy: zeměpisná šířka a délka (int32, 1e-7 stupně)
        static void ProcessPositionRapidUpdate(byte[] data)
        {
            if (data.Length < 8) return;

            double latitude = BitConverter.ToInt32(data, 0) * 1e-7;
            double longitude = BitConverter.ToInt32(data, 4) * 1e-7;

            Console.WriteLine($"Pozice: {latitude:F6}°, {longitude:F6}°");
        }

        // PGN 129026 - kurz (COG) a rychlost (SOG) vůči zemi
        static void ProcessCogSogRapidUpdate(byte[] data)
        {
            if (data.Length < 6) return;

            // Struktura: SID (1 B), reference COG (1 B), COG (uint16, 0.0001 rad), SOG (uint16, 0.01 m/s)
            double cog = BitConverter.ToUInt16(data, 2) * 0.0001 * RadToDeg; // Course Over Ground
            double sog = BitConverter.ToUInt16(data, 4) * 0.01 * MsToKnots;  // Speed Over Ground

            Console.WriteLine($"Kurz: {cog:F2}°");
            Console.WriteLine($"Rychlost: {sog:F2} uzlů");
        }

        // PGN 129539 - přesnost určení polohy (DOP, menší hodnota = lepší)
        static void ProcessGnssDops(byte[] data)
        {
            if (data.Length < 8) return;

            // Struktura: SID (1 B), režim (1 B), HDOP, VDOP, TDOP (int16, 0.01)
            short hdop = BitConverter.ToInt16(data, 2);
            short vdop = BitConverter.ToInt16(data, 4);
            short tdop = BitConverter.ToInt16(data, 6);

            Console.WriteLine($"HDOP: {hdop * 0.01:F2}");
            Console.WriteLine($"VDOP: {vdop * 0.01:F2}");
            Console.WriteLine($"TDOP: {tdop * 0.01:F2}");
        }

        // PGN 129033 - datum a čas (UTC)
        static void ProcessTimeDate(byte[] data)
        {
            if (data.Length < 6) return;

            // Struktura: datum (uint16, dny od 1.1.1970), čas (uint32, 0.0001 s od půlnoci UTC)
            ushort daysSince1970 = BitConverter.ToUInt16(data, 0);
            uint secondsSinceMidnight = BitConverter.ToUInt32(data, 2);

            DateTime date = new DateTime(1970, 1, 1).AddDays(daysSince1970)
                .AddSeconds(secondsSinceMidnight * 0.0001);

            Console.WriteLine($"Datum a čas: {date:dd.MM.yyyy HH:mm:ss.fff} UTC");
        }

        // PGN 127250 - směr lodi (heading)
        static void ProcessVesselHeading(byte[] data)
        {
            if (data.Length < 7) return;

            // Struktura: SID (1 B), heading (uint16), deviace (int16), variace (int16), všechny v 0.0001 rad
            double heading = BitConverter.ToUInt16(data, 1) * 0.0001 * RadToDeg;
            double deviation = BitConverter.ToInt16(data, 3) * 0.0001 * RadToDeg;
            double variation = BitConverter.ToInt16(data, 5) * 0.0001 * RadToDeg;

            Console.WriteLine($"Směr: {heading:F2}°");
            Console.WriteLine($"Deviace: {deviation:F2}°");
            Console.WriteLine($"Variation: {variation:F2}°");
        }

        // PGN 128259 - rychlost lodi vůči vodě a vůči zemi
        static void ProcessSpeed(byte[] data)
        {
            if (data.Length < 5) return;

            // Struktura: SID (1 B), rychlost vůči vodě (uint16, 0.01 m/s), rychlost vůči zemi (uint16, 0.01 m/s)
            double speedWaterReferenced = BitConverter.ToUInt16(data, 1) * 0.01 * MsToKnots;
            double speedGroundReferenced = BitConverter.ToUInt16(data, 3) * 0.01 * MsToKnots;

            Console.WriteLine($"Rychlost vůči vodě: {speedWaterReferenced:F2} uzlů");
            Console.WriteLine($"Rychlost vůči zemi: {speedGroundReferenced:F2} uzlů");
        }

        // PGN 128267 - hloubka vody pod čidlem
        static void ProcessWaterDepth(byte[] data)
        {
            if (data.Length < 7) return;

            // Struktura: SID (1 B), hloubka (uint32, 0.01 m), offset (int16, 0.001 m)
            double depth = BitConverter.ToUInt32(data, 1) * 0.01;
            double offset = BitConverter.ToInt16(data, 5) * 0.001;

            Console.WriteLine($"Hloubka: {depth:F2} m");
            Console.WriteLine($"Offset: {offset:F3} m");
        }

        // PGN 130306 - údaje o větru
        static void ProcessWindData(byte[] data)
        {
            if (data.Length < 6) return;

            // Struktura: SID (1 B), rychlost (uint16, 0.01 m/s), úhel (uint16, 0.0001 rad), reference (3 bity)
            double windSpeed = BitConverter.ToUInt16(data, 1) * 0.01 * MsToKnots;
            double windAngle = BitConverter.ToUInt16(data, 3) * 0.0001 * RadToDeg;
            byte reference = (byte)(data[5] & 0x07);

            string referenceStr = reference switch
            {
                0 => "Skutečný (vůči zemi, sever)",
                1 => "Magnetický",
                2 => "Zdánlivý",
                3 => "Skutečný (vůči lodi)",
                4 => "Skutečný (vůči vodě)",
                _ => "Neznámý"
            };

            Console.WriteLine($"Rychlost větru: {windSpeed:F1} uzlů");
            Console.WriteLine($"Úhel větru: {windAngle:F1}°");
            Console.WriteLine($"Reference: {referenceStr}");
        }

        // Pomocné metody pro výpis (v aktuální verzi programu se nepoužívají, slouží jako příklad pro další rozšíření)
        // Převod pole bajtů na hexadecimální řetězec oddělený mezerami
        static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        // Formátování souřadnice se světovou stranou (N/S pro šířku, E/W pro délku)
        static string FormatCoordinate(double value, bool isLatitude)
        {
            char direction = isLatitude ?
                (value >= 0 ? 'N' : 'S') :
                (value >= 0 ? 'E' : 'W');

            return $"{Math.Abs(value):F6}° {direction}";
        }
    }
}
/*
NMEA 2000:
 Sběrnice založená na CAN (250 kbit/s) pro lodní elektroniku; zprávy se rozlišují čísly PGN (Parameter Group Number)
 Na rozdíl od textového NMEA 0183 je protokol binární - hodnoty jsou celá čísla s pevným násobitelem (rozlišením)
Zjednodušení v této ukázce:
 Vstup ze sériové linky má vymyšlený formát [PGN 3 B][zdroj 1 B][data max. 8 B]
 Fast-packet zprávy (více než 8 bajtů, např. PGN 129029) se neskládají
Jednotky v NMEA 2000:
 Úhly jsou v radiánech (program je převádí na stupně), rychlosti v m/s (převod na uzly)
 Souřadnice v PGN 129025 jsou v jednotkách 1e-7 stupně
*/
