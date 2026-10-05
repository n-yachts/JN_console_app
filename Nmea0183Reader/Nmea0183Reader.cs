using System;  // Základní jmenný prostor pro Console, Exception a základní třídy
using System.Globalization;  // CultureInfo.InvariantCulture - desetinná tečka bez ohledu na nastavení systému
using System.IO.Ports;  // Práce se sériovým portem (SerialPort)

namespace Nmea0183Reader  // Jmenný prostor projektu
{
    class Nmea0183Reader  // Hlavní třída programu - čtečka NMEA 0183 vět ze sériové linky (GPS přijímač)
    {
        private static SerialPort _serialPort;  // Sériový port sdílený mezi metodami (statický, protože Main je statický)

        static void Main(string[] args)  // Hlavní vstupní bod programu
        {
            Console.WriteLine("NMEA 0183 Reader - Sériová linka");

            // Nastavení sériového portu
            _serialPort = new SerialPort();
            ConfigureSerialPort();

            try  // Ošetření chyb při otevírání portu (neexistující port, port používaný jinou aplikací)
            {
                _serialPort.Open();
                Console.WriteLine($"Připojeno k {_serialPort.PortName}, {_serialPort.BaudRate} baud");
                Console.WriteLine("Čtení dat... Stiskněte 'q' pro ukončení.\n");

                // Registrace obsluhy události - zavolá se na samostatném vlákně vždy, když dorazí data
                _serialPort.DataReceived += SerialPort_DataReceived;

                // Hlavní smyčka pro ukončení programu (zpracování dat probíhá v události DataReceived)
                while (true)
                {
                    var key = Console.ReadKey(true);  // Čekání na stisk klávesy bez zobrazení znaku
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");
            }
            finally  // Uvolnění portu proběhne vždy (i při chybě)
            {
                if (_serialPort?.IsOpen == true)
                    _serialPort.Close();
                _serialPort?.Dispose();
            }

            Console.WriteLine("\nProgram ukončen. Stiskněte libovolnou klávesu...");
            Console.ReadKey();
        }

        // Dotaz na uživatele a nastavení parametrů sériového portu
        static void ConfigureSerialPort()
        {
            Console.WriteLine("Dostupné sériové porty:");
            string[] ports = SerialPort.GetPortNames();  // Seznam portů dostupných v systému (COM1, COM3, ...)
            foreach (string port in ports)
            {
                Console.WriteLine($" - {port}");
            }

            Console.Write("Zadejte název portu (např. COM3, na Linuxu /dev/ttyUSB0): ");
            _serialPort.PortName = Console.ReadLine();

            // NMEA 0183 používá standardně rychlost 4800 baud (některé přijímače 9600 a více)
            Console.Write("Zadejte baud rate (výchozí 4800): ");
            if (int.TryParse(Console.ReadLine(), out int baudRate))
                _serialPort.BaudRate = baudRate;
            else
                _serialPort.BaudRate = 4800;

            // Standardní formát NMEA 0183: 8 datových bitů, bez parity, 1 stop bit (8N1), bez řízení toku
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 1000;   // Časový limit čtení v ms
            _serialPort.WriteTimeout = 1000;  // Časový limit zápisu v ms
        }

        // Obsluha události - zavolá se, když na sériový port dorazí data
        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Čtení po řádcích, dokud jsou v bufferu nějaká data
                while (_serialPort.BytesToRead > 0)
                {
                    string line = _serialPort.ReadLine();  // Jedna NMEA věta (končí CR LF)

                    // Odstranění CR/LF znaků
                    line = line.TrimEnd('\r', '\n');

                    // Zpracují se jen věty se správným tvarem a kontrolním součtem
                    if (IsValidNmeaLine(line))
                    {
                        ProcessNmeaLine(line);
                    }
                }
            }
            catch (TimeoutException) { }  // Nekompletní řádek - dočte se při dalším příchodu dat
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení dat: {ex.Message}");
            }
        }

        // Kontrola, zda je řádek platná NMEA věta: začíná '$', obsahuje '*' a sedí kontrolní součet
        static bool IsValidNmeaLine(string line)
        {
            return !string.IsNullOrEmpty(line) &&
                   line.StartsWith("$") &&
                   line.Contains("*") &&
                   CheckChecksum(line);
        }

        // Ověření kontrolního součtu - dvě hex číslice za znakem '*'
        static bool CheckChecksum(string line)
        {
            try
            {
                int asteriskPos = line.IndexOf('*');  // Pozice hvězdičky oddělující data a kontrolní součet
                if (asteriskPos < 0 || asteriskPos + 3 > line.Length)
                    return false;

                string data = line.Substring(1, asteriskPos - 1);  // Data mezi '$' a '*' (bez nich)
                string checksum = line.Substring(asteriskPos + 1, 2);  // Přijatý kontrolní součet (2 hex znaky)

                byte calculatedChecksum = CalculateChecksum(data);  // Vypočtený kontrolní součet
                // Porovnání bez ohledu na velikost písmen (některá zařízení posílají malá písmena)
                return string.Equals(calculatedChecksum.ToString("X2"), checksum, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;  // Jakákoli chyba = neplatná věta
            }
        }

        // Kontrolní součet NMEA = XOR všech znaků mezi '$' a '*'
        static byte CalculateChecksum(string data)
        {
            byte checksum = 0;
            foreach (char c in data)
            {
                checksum ^= (byte)c;
            }
            return checksum;
        }

        // Rozdělení věty na pole a zpracování podle typu věty
        static void ProcessNmeaLine(string line)
        {
            // Odstranění kontrolního součtu (*hh), aby se nezobrazoval v posledním poli věty
            line = line.Substring(0, line.IndexOf('*'));

            string[] parts = line.Split(',');  // Pole jsou oddělena čárkami
            if (parts.Length == 0) return;

            // První pole je např. "$GPGGA": 2 znaky jsou identifikátor zařízení (GP = GPS), zbytek je typ věty
            string sentenceType = parts[0].Length >= 3 ? parts[0].Substring(3) : "";

            switch (sentenceType)
            {
                case "GGA":  // Fix data (pozice, výška, kvalita)
                    ProcessGGA(parts);
                    break;
                case "RMC":  // Doporučená minimální data (pozice, rychlost, datum)
                    ProcessRMC(parts);
                    break;
                case "GSA":  // DOP a aktivní satelity
                    ProcessGSA(parts);
                    break;
                case "GSV":  // Satelity v dohledu
                    ProcessGSV(parts);
                    break;
                case "VTG":  // Kurz a rychlost vůči zemi
                    ProcessVTG(parts);
                    break;
                default:
                    // Pro ostatní zprávy vypišeme pouze typ
                    if (!string.IsNullOrEmpty(sentenceType))
                        Console.WriteLine($"Přijata zpráva: {sentenceType}");
                    break;
            }
        }

        // GGA - Global Positioning System Fix Data
        static void ProcessGGA(string[] parts)
        {
            // $--GGA,time,lat,NS,lon,EW,quality,numSat,HDOP,alt,M,geoid,M,diffAge,diffStation*cs
            if (parts.Length < 15) return;  // Neúplná věta

            Console.WriteLine("\n--- GGA Zpráva ---");
            Console.WriteLine($"Čas: {ParseTime(parts[1])}");
            Console.WriteLine($"Pozice: {ParseLatitude(parts[2], parts[3])} {ParseLongitude(parts[4], parts[5])}");
            Console.WriteLine($"Kvalita signálu: {ParseSignalQuality(parts[6])}");
            Console.WriteLine($"Počet satelitů: {parts[7]}");
            Console.WriteLine($"HDOP: {parts[8]}");  // Horizontální snížení přesnosti (menší = lepší)
            Console.WriteLine($"Nadmořská výška: {parts[9]} {parts[10]}");
            Console.WriteLine($"Výška geoidu: {parts[11]} {parts[12]}");
        }

        // RMC - Recommended Minimum Navigation Information
        static void ProcessRMC(string[] parts)
        {
            // $--RMC,time,status,lat,NS,lon,EW,speed,course,date,magVar,EW*cs
            if (parts.Length < 12) return;  // Neúplná věta

            Console.WriteLine("\n--- RMC Zpráva ---");
            Console.WriteLine($"Čas: {ParseTime(parts[1])}");
            Console.WriteLine($"Stav: {parts[2]}");  // A = platná data, V = varování (neplatná)
            Console.WriteLine($"Pozice: {ParseLatitude(parts[3], parts[4])} {ParseLongitude(parts[5], parts[6])}");
            Console.WriteLine($"Rychlost: {ParseSpeed(parts[7])} uzlů ({ParseSpeedToKmh(parts[7])} km/h)");
            Console.WriteLine($"Směr: {parts[8]}°");
            Console.WriteLine($"Datum: {ParseDate(parts[9])}");

            // Magnetická deklinace nemusí být vyplněna
            if (parts.Length > 10 && !string.IsNullOrEmpty(parts[10]))
                Console.WriteLine($"Magnetická deklinace: {parts[10]}° {parts[11]}");
        }

        // GSA - GNSS DOP and Active Satellites
        static void ProcessGSA(string[] parts)
        {
            // $--GSA,mode,fix,sv1,sv2,...,sv12,pdop,hdop,vdop*cs
            // Smyčka níže čte pole 3-14, věta musí mít minimálně 15 polí
            if (parts.Length < 15) return;

            Console.WriteLine("\n--- GSA Zpráva ---");
            Console.WriteLine($"Režim: {parts[1]}");  // M = manuální, A = automatický
            Console.WriteLine($"Typ fixu: {ParseFixType(parts[2])}");

            // Pole 3-14 jsou čísla satelitů použitých pro výpočet pozice (nevyplněná pole se nepočítají)
            int satCount = 0;
            for (int i = 3; i <= 14; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]) && parts[i] != "0")
                    satCount++;
            }
            Console.WriteLine($"Použité satelity: {satCount}");

            if (parts.Length > 15) Console.WriteLine($"PDOP: {parts[15]}");  // Prostorové DOP
            if (parts.Length > 16) Console.WriteLine($"HDOP: {parts[16]}");  // Horizontální DOP
            if (parts.Length > 17) Console.WriteLine($"VDOP: {parts[17]}");  // Vertikální DOP
        }

        // GSV - GNSS Satellites in View (jedna věta obsahuje max. 4 satelity)
        static void ProcessGSV(string[] parts)
        {
            // $--GSV,msgCount,msgNo,satCount,sat1prn,sat1el,sat1az,sat1snr,...*cs
            if (parts.Length < 4) return;

            Console.WriteLine($"\n--- GSV Zpráva {parts[2]}/{parts[1]} ---");  // číslo zprávy / celkem zpráv
            Console.WriteLine($"Celkem satelitů: {parts[3]}");

            // Každý satelit zabírá 4 pole: číslo (PRN), elevace, azimut, poměr signál/šum (SNR)
            for (int i = 4; i + 3 < parts.Length; i += 4)
            {
                if (!string.IsNullOrEmpty(parts[i]) && parts[i] != "0")
                {
                    Console.WriteLine($" Sat {parts[i]}: elev {parts[i + 1]}°, azimut {parts[i + 2]}°, SNR {parts[i + 3]}");
                }
            }
        }

        // VTG - Course Over Ground and Ground Speed
        static void ProcessVTG(string[] parts)
        {
            // $--VTG,courseTrue,T,courseMag,M,speedN,kN,speedK,km/h,mode*cs
            if (parts.Length < 9) return;

            Console.WriteLine("\n--- VTG Zpráva ---");
            Console.WriteLine($"Skutečný směr: {parts[1]}°");
            if (!string.IsNullOrEmpty(parts[3]))
                Console.WriteLine($"Magnetický směr: {parts[3]}°");
            Console.WriteLine($"Rychlost: {parts[5]} uzlů, {parts[7]} km/h");
        }

        // Čas ve formátu hhmmss.sss -> hh:mm:ss UTC
        static string ParseTime(string time)
        {
            if (string.IsNullOrEmpty(time) || time.Length < 6) return "Neplatný čas";
            return $"{time.Substring(0, 2)}:{time.Substring(2, 2)}:{time.Substring(4, 2)} UTC";
        }

        // Datum ve formátu ddmmyy -> dd.mm.yy
        static string ParseDate(string date)
        {
            if (string.IsNullOrEmpty(date) || date.Length < 6) return "Neplatné datum";
            return $"{date.Substring(0, 2)}.{date.Substring(2, 2)}.{date.Substring(4, 2)}";
        }

        // Zeměpisná šířka ve formátu ddmm.mmmm (stupně + minuty) -> desetinné stupně
        static string ParseLatitude(string lat, string ns)
        {
            if (string.IsNullOrEmpty(lat) || lat.Length < 4) return "0";
            try
            {
                double degrees = double.Parse(lat.Substring(0, 2), CultureInfo.InvariantCulture);  // První 2 znaky = stupně
                double minutes = double.Parse(lat.Substring(2), CultureInfo.InvariantCulture);  // Zbytek = minuty
                return $"{degrees + minutes / 60:0.000000}° {ns}";  // 60 minut = 1 stupeň
            }
            catch
            {
                return "Chyba parsování";
            }
        }

        // Zeměpisná délka ve formátu dddmm.mmmm (stupně + minuty) -> desetinné stupně
        static string ParseLongitude(string lon, string ew)
        {
            if (string.IsNullOrEmpty(lon) || lon.Length < 5) return "0";
            try
            {
                double degrees = double.Parse(lon.Substring(0, 3), CultureInfo.InvariantCulture);  // První 3 znaky = stupně
                double minutes = double.Parse(lon.Substring(3), CultureInfo.InvariantCulture);  // Zbytek = minuty
                return $"{degrees + minutes / 60:0.000000}° {ew}";
            }
            catch
            {
                return "Chyba parsování";
            }
        }

        // Kód kvality určení polohy z věty GGA
        static string ParseSignalQuality(string quality)
        {
            return quality switch
            {
                "0" => "Neplatný",
                "1" => "GPS fix",
                "2" => "DGPS fix",
                "3" => "PPS fix",
                "4" => "RTK",
                "5" => "Float RTK",
                "6" => "Odhadnutý",
                "7" => "Manuální",
                "8" => "Simulace",
                _ => "Neznámý"
            };
        }

        // Typ fixu z věty GSA
        static string ParseFixType(string fix)
        {
            return fix switch
            {
                "1" => "Žádný",
                "2" => "2D",
                "3" => "3D",
                _ => "Neznámý"
            };
        }

        // Rychlost v uzlech zaokrouhlená na 1 desetinné místo
        static string ParseSpeed(string speed)
        {
            return double.TryParse(speed, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result.ToString("0.0")
                : "0.0";
        }

        // Převod rychlosti z uzlů na km/h (1 uzel = 1,852 km/h)
        static string ParseSpeedToKmh(string speedKnots)
        {
            if (double.TryParse(speedKnots, NumberStyles.Any, CultureInfo.InvariantCulture, out double knots))
            {
                double kmh = knots * 1.852;
                return kmh.ToString("0.0");
            }
            return "0.0";
        }
    }
}

/*
NMEA 0183:
 Textový protokol pro námořní a GPS zařízení, každá věta je jeden řádek: $TTSSS,pole1,pole2,...*hh
 TT = identifikátor zařízení (GP = GPS), SSS = typ věty (GGA, RMC, ...), hh = kontrolní součet
Kontrolní součet:
 XOR všech znaků mezi '$' a '*', zapsaný jako dvě hex číslice
Zpracované věty:
 GGA (poloha a kvalita), RMC (minimální navigační data), GSA (DOP a použité satelity),
 GSV (satelity v dohledu), VTG (kurz a rychlost)
Souřadnice:
 Šířka ddmm.mmmm, délka dddmm.mmmm (stupně a minuty), převádí se na desetinné stupně
Sériová linka:
 Standardně 4800 baud, 8N1; příjem dat probíhá v události DataReceived na samostatném vlákně
*/
