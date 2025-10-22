using System;
using System.Globalization;
using System.IO.Ports;

namespace Nmea0183Reader
{
    class Nmea0183Reader
    {
        private static SerialPort _serialPort;

        static void Main(string[] args)
        {
            Console.WriteLine("NMEA 0183 Reader - Sériová linka");

            // Nastavení sériového portu
            _serialPort = new SerialPort();
            ConfigureSerialPort();

            try
            {
                _serialPort.Open();
                Console.WriteLine($"Připojeno k {_serialPort.PortName}, {_serialPort.BaudRate} baud");
                Console.WriteLine("Čtení dat... Stiskněte 'q' pro ukončení.\n");

                _serialPort.DataReceived += SerialPort_DataReceived;

                // Hlavní smyčka pro ukončení programu
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
            finally
            {
                if (_serialPort?.IsOpen == true)
                    _serialPort.Close();
                _serialPort?.Dispose();
            }

            Console.WriteLine("\nProgram ukončen. Stiskněte libovolnou klávesu...");
            Console.ReadKey();
        }

        static void ConfigureSerialPort()
        {
            Console.WriteLine("Dostupné sériové porty:");
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                Console.WriteLine($" - {port}");
            }

            Console.Write("Zadejte název portu (např. COM3): ");
            _serialPort.PortName = Console.ReadLine();

            Console.Write("Zadejte baud rate (výchozí 4800): ");
            if (int.TryParse(Console.ReadLine(), out int baudRate))
                _serialPort.BaudRate = baudRate;
            else
                _serialPort.BaudRate = 4800;

            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 1000;
            _serialPort.WriteTimeout = 1000;
        }

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                while (_serialPort.BytesToRead > 0)
                {
                    string line = _serialPort.ReadLine();

                    // Odstranění CR/LF znaků
                    line = line.TrimEnd('\r', '\n');

                    if (IsValidNmeaLine(line))
                    {
                        ProcessNmeaLine(line);
                    }
                }
            }
            catch (TimeoutException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení dat: {ex.Message}");
            }
        }

        static bool IsValidNmeaLine(string line)
        {
            return !string.IsNullOrEmpty(line) &&
                   line.StartsWith("$") &&
                   line.Contains("*") &&
                   CheckChecksum(line);
        }

        static bool CheckChecksum(string line)
        {
            try
            {
                int asteriskPos = line.IndexOf('*');
                if (asteriskPos < 0 || asteriskPos + 3 > line.Length)
                    return false;

                string data = line.Substring(1, asteriskPos - 1);
                string checksum = line.Substring(asteriskPos + 1, 2);

                byte calculatedChecksum = CalculateChecksum(data);
                return calculatedChecksum.ToString("X2") == checksum;
            }
            catch
            {
                return false;
            }
        }

        static byte CalculateChecksum(string data)
        {
            byte checksum = 0;
            foreach (char c in data)
            {
                checksum ^= (byte)c;
            }
            return checksum;
        }

        static void ProcessNmeaLine(string line)
        {
            string[] parts = line.Split(',');
            if (parts.Length == 0) return;

            string sentenceType = parts[0].Length >= 3 ? parts[0].Substring(3) : "";

            switch (sentenceType)
            {
                case "GGA":
                    ProcessGGA(parts);
                    break;
                case "RMC":
                    ProcessRMC(parts);
                    break;
                case "GSA":
                    ProcessGSA(parts);
                    break;
                case "GSV":
                    ProcessGSV(parts);
                    break;
                case "VTG":
                    ProcessVTG(parts);
                    break;
                default:
                    // Pro ostatní zprávy vypišeme pouze typ
                    if (!string.IsNullOrEmpty(sentenceType))
                        Console.WriteLine($"Přijata zpráva: {sentenceType}");
                    break;
            }
        }

        static void ProcessGGA(string[] parts)
        {
            // $--GGA,time,lat,NS,lon,EW,quality,numSat,HDOP,alt,M,geoid,M,diffAge,diffStation*cs
            if (parts.Length < 15) return;

            Console.WriteLine("\n--- GGA Zpráva ---");
            Console.WriteLine($"Čas: {ParseTime(parts[1])}");
            Console.WriteLine($"Pozice: {ParseLatitude(parts[2], parts[3])} {ParseLongitude(parts[4], parts[5])}");
            Console.WriteLine($"Kvalita signálu: {ParseSignalQuality(parts[6])}");
            Console.WriteLine($"Počet satelitů: {parts[7]}");
            Console.WriteLine($"HDOP: {parts[8]}");
            Console.WriteLine($"Nadmořská výška: {parts[9]} {parts[10]}");
            Console.WriteLine($"Výška geoidu: {parts[11]} {parts[12]}");
        }

        static void ProcessRMC(string[] parts)
        {
            // $--RMC,time,status,lat,NS,lon,EW,speed,course,date,magVar,EW*cs
            if (parts.Length < 12) return;

            Console.WriteLine("\n--- RMC Zpráva ---");
            Console.WriteLine($"Čas: {ParseTime(parts[1])}");
            Console.WriteLine($"Stav: {parts[2]}");
            Console.WriteLine($"Pozice: {ParseLatitude(parts[3], parts[4])} {ParseLongitude(parts[5], parts[6])}");
            Console.WriteLine($"Rychlost: {ParseSpeed(parts[7])} uzlů ({ParseSpeedToKmh(parts[7])} km/h)");
            Console.WriteLine($"Směr: {parts[8]}°");
            Console.WriteLine($"Datum: {ParseDate(parts[9])}");

            if (parts.Length > 10 && !string.IsNullOrEmpty(parts[10]))
                Console.WriteLine($"Magnetická deklinace: {parts[10]}° {parts[11]}");
        }

        static void ProcessGSA(string[] parts)
        {
            // $--GSA,mode,fix,sv1,sv2,...,pdop,hdop,vdop*cs
            if (parts.Length < 6) return;

            Console.WriteLine("\n--- GSA Zpráva ---");
            Console.WriteLine($"Režim: {parts[1]}");
            Console.WriteLine($"Typ fixu: {ParseFixType(parts[2])}");

            int satCount = 0;
            for (int i = 3; i <= 14; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]) && parts[i] != "0")
                    satCount++;
            }
            Console.WriteLine($"Použité satelity: {satCount}");

            if (parts.Length > 15) Console.WriteLine($"PDOP: {parts[15]}");
            if (parts.Length > 16) Console.WriteLine($"HDOP: {parts[16]}");
            if (parts.Length > 17) Console.WriteLine($"VDOP: {parts[17]}");
        }

        static void ProcessGSV(string[] parts)
        {
            // $--GSV,msgCount,msgNo,satCount,sat1prn,sat1el,sat1az,sat1snr,...*cs
            if (parts.Length < 4) return;

            Console.WriteLine($"\n--- GSV Zpráva {parts[2]}/{parts[1]} ---");
            Console.WriteLine($"Celkem satelitů: {parts[3]}");

            for (int i = 4; i + 3 < parts.Length; i += 4)
            {
                if (!string.IsNullOrEmpty(parts[i]) && parts[i] != "0")
                {
                    Console.WriteLine($" Sat {parts[i]}: elev {parts[i + 1]}°, azimut {parts[i + 2]}°, SNR {parts[i + 3]}");
                }
            }
        }

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

        static string ParseTime(string time)
        {
            if (string.IsNullOrEmpty(time) || time.Length < 6) return "Neplatný čas";
            return $"{time.Substring(0, 2)}:{time.Substring(2, 2)}:{time.Substring(4, 2)} UTC";
        }

        static string ParseDate(string date)
        {
            if (string.IsNullOrEmpty(date) || date.Length < 6) return "Neplatné datum";
            return $"{date.Substring(0, 2)}.{date.Substring(2, 2)}.{date.Substring(4, 2)}";
        }

        static string ParseLatitude(string lat, string ns)
        {
            if (string.IsNullOrEmpty(lat) || lat.Length < 4) return "0";
            try
            {
                double degrees = double.Parse(lat.Substring(0, 2), CultureInfo.InvariantCulture);
                double minutes = double.Parse(lat.Substring(2), CultureInfo.InvariantCulture);
                return $"{degrees + minutes / 60:0.000000}° {ns}";
            }
            catch
            {
                return "Chyba parsování";
            }
        }

        static string ParseLongitude(string lon, string ew)
        {
            if (string.IsNullOrEmpty(lon) || lon.Length < 5) return "0";
            try
            {
                double degrees = double.Parse(lon.Substring(0, 3), CultureInfo.InvariantCulture);
                double minutes = double.Parse(lon.Substring(3), CultureInfo.InvariantCulture);
                return $"{degrees + minutes / 60:0.000000}° {ew}";
            }
            catch
            {
                return "Chyba parsování";
            }
        }

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

        static string ParseSpeed(string speed)
        {
            return double.TryParse(speed, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
                ? result.ToString("0.0")
                : "0.0";
        }

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