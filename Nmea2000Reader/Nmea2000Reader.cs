using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace Nmea2000Reader
{
    class Nmea2000Reader
    {
        private static SerialPort _serialPort;
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
            { 129027, "GNSS Position Data" },
            { 129029, "GNSS DOPs" },
            { 129033, "Time & Date" },
            { 129539, "GNSS Satellites in View" },
            { 130306, "Wind Data" },
            { 130310, "Environmental Parameters" },
            { 130311, "Temperature" },
            { 130312, "Pressure" },
            { 130313, "Humidity" },
            { 130314, "Actual Salinity" }
        };

        static void Main(string[] args)
        {
            Console.WriteLine("NMEA 2000 Reader");
            Console.WriteLine("================\n");

            _serialPort = new SerialPort();

            if (!ConfigureSerialPort())
            {
                Console.WriteLine("Nepodařilo se nakonfigurovat sériový port.");
                return;
            }

            try
            {
                _serialPort.Open();
                Console.WriteLine($"Připojeno k {_serialPort.PortName}, {_serialPort.BaudRate} baud");
                Console.WriteLine("Čtení NMEA 2000 dat... Stiskněte 'q' pro ukončení.\n");

                _serialPort.DataReceived += SerialPort_DataReceived;

                // Hlavní smyčka
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

        static bool ConfigureSerialPort()
        {
            try
            {
                string[] ports = SerialPort.GetPortNames();

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

                if (int.TryParse(input, out int portNumber) && portNumber >= 1 && portNumber <= ports.Length)
                {
                    _serialPort.PortName = ports[portNumber - 1];
                }
                else
                {
                    _serialPort.PortName = input;
                }

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

        private static void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0) return;

                byte[] buffer = new byte[bytesToRead];
                _serialPort.Read(buffer, 0, bytesToRead);

                ProcessNmea2000Data(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení dat: {ex.Message}");
            }
        }

        static void ProcessNmea2000Data(byte[] data)
        {
            // NMEA 2000 používá CAN bus frame formát
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
                }
            }
        }

        static void ProcessN2kMessage(byte[] data, ref int index)
        {
            // ZÁKLADNÍ PARSOVÁNÍ NMEA 2000 ZPRÁVY
            // Toto je zjednodušená implementace - reálná implementace by byla komplexnější

            if (index + 3 >= data.Length) return;

            // Předpokládáme, že data obsahují kompletní N2K zprávy
            int startIndex = index;

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
                    case 129027: // GNSS Position Data
                        ProcessGnssPositionData(data);
                        break;
                    case 129029: // GNSS DOPs
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

        static void ProcessPositionRapidUpdate(byte[] data)
        {
            if (data.Length < 8) return;

            double latitude = BitConverter.ToInt32(data, 0) * 1e-7;
            double longitude = BitConverter.ToInt32(data, 4) * 1e-7;

            Console.WriteLine($"Pozice: {latitude:F6}°, {longitude:F6}°");
        }

        static void ProcessCogSogRapidUpdate(byte[] data)
        {
            if (data.Length < 6) return;

            double cog = BitConverter.ToUInt16(data, 0) * 0.0001; // Course Over Ground
            double sog = BitConverter.ToUInt16(data, 2) * 0.01;   // Speed Over Ground

            Console.WriteLine($"Kurz: {cog:F2}°");
            Console.WriteLine($"Rychlost: {sog:F2} uzlů");
        }

        static void ProcessGnssPositionData(byte[] data)
        {
            if (data.Length < 20) return;

            double latitude = BitConverter.ToInt32(data, 4) * 1e-16 * 180.0 / Math.PI;
            double longitude = BitConverter.ToInt32(data, 8) * 1e-16 * 180.0 / Math.PI;
            double altitude = BitConverter.ToSingle(data, 12);
            byte satellites = data[16];

            Console.WriteLine($"GPS Pozice: {latitude:F6}°, {longitude:F6}°");
            Console.WriteLine($"Nadmořská výška: {altitude:F1} m");
            Console.WriteLine($"Satelity: {satellites}");
        }

        static void ProcessGnssDops(byte[] data)
        {
            if (data.Length < 5) return;

            ushort hdop = BitConverter.ToUInt16(data, 0);
            ushort vdop = BitConverter.ToUInt16(data, 2);
            byte positionDop = data[4];

            Console.WriteLine($"HDOP: {hdop * 0.01:F2}");
            Console.WriteLine($"VDOP: {vdop * 0.01:F2}");
            Console.WriteLine($"PDOP: {positionDop * 0.01:F2}");
        }

        static void ProcessTimeDate(byte[] data)
        {
            if (data.Length < 8) return;

            uint daysSince1970 = BitConverter.ToUInt32(data, 0);
            uint secondsSinceMidnight = BitConverter.ToUInt32(data, 4);

            DateTime date = new DateTime(1970, 1, 1).AddDays(daysSince1970)
                .AddSeconds(secondsSinceMidnight * 0.0001);

            Console.WriteLine($"Datum a čas: {date:dd.MM.yyyy HH:mm:ss.fff}");
        }

        static void ProcessVesselHeading(byte[] data)
        {
            if (data.Length < 8) return;

            double heading = BitConverter.ToUInt16(data, 0) * 0.0001;
            double deviation = BitConverter.ToInt16(data, 2) * 0.0001;
            double variation = BitConverter.ToInt16(data, 4) * 0.0001;

            Console.WriteLine($"Směr: {heading:F2}°");
            Console.WriteLine($"Deviace: {deviation:F2}°");
            Console.WriteLine($"Variation: {variation:F2}°");
        }

        static void ProcessSpeed(byte[] data)
        {
            if (data.Length < 8) return;

            double speedWaterReferenced = BitConverter.ToUInt16(data, 0) * 0.01;
            double speedGroundReferenced = BitConverter.ToUInt16(data, 2) * 0.01;

            Console.WriteLine($"Rychlost vůči vodě: {speedWaterReferenced:F2} uzlů");
            Console.WriteLine($"Rychlost vůči zemi: {speedGroundReferenced:F2} uzlů");
        }

        static void ProcessWaterDepth(byte[] data)
        {
            if (data.Length < 8) return;

            double depth = BitConverter.ToUInt32(data, 0) * 0.01;
            double offset = BitConverter.ToUInt16(data, 4) * 0.01;

            Console.WriteLine($"Hloubka: {depth:F2} m");
            Console.WriteLine($"Offset: {offset:F2} m");
        }

        static void ProcessWindData(byte[] data)
        {
            if (data.Length < 8) return;

            double windSpeed = BitConverter.ToUInt16(data, 0) * 0.01;
            double windAngle = BitConverter.ToUInt16(data, 2) * 0.0001;
            byte reference = data[4];

            string referenceStr = reference switch
            {
                0 => "Skutečný",
                1 => "Zdánlivý",
                _ => "Neznámý"
            };

            Console.WriteLine($"Rychlost větru: {windSpeed:F1} uzlů");
            Console.WriteLine($"Úhel větru: {windAngle:F1}°");
            Console.WriteLine($"Reference: {referenceStr}");
        }

        // Pomocné metody pro výpis
        static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        static string FormatCoordinate(double value, bool isLatitude)
        {
            char direction = isLatitude ?
                (value >= 0 ? 'N' : 'S') :
                (value >= 0 ? 'E' : 'W');

            return $"{Math.Abs(value):F6}° {direction}";
        }
    }
}