using System;  // Import základních systémových funkcí a tříd
using System.Collections.Generic;  // Import slovníku se jmény OID
using System.Net;  // Import síťových funkcí (IPEndPoint, IPAddress)
using System.Net.Sockets;  // Import UDP klienta pro příjem trapů
using System.Text;  // Import práce s textovými kódováními
using System.Threading;  // Import CancellationTokenSource pro ukončení přes Ctrl+C
using System.Threading.Tasks;  // Import asynchronního programování

class SnmpTrapReceiver  // Hlavní třída - přijímač SNMP trapů (v1 a v2c)
{
    // Názvy známých OID (kmenové OID bez instance); za nalezeným kmenem se ponechá zbytek, např. ifOperStatus.3
    static readonly Dictionary<string, string> KnownOids = new Dictionary<string, string>
    {
        { "1.3.6.1.2.1.1.1", "sysDescr" },
        { "1.3.6.1.2.1.1.3", "sysUpTime" },
        { "1.3.6.1.2.1.1.5", "sysName" },
        { "1.3.6.1.2.1.2.2.1.1", "ifIndex" },
        { "1.3.6.1.2.1.2.2.1.2", "ifDescr" },
        { "1.3.6.1.2.1.2.2.1.7", "ifAdminStatus" },
        { "1.3.6.1.2.1.2.2.1.8", "ifOperStatus" },
        { "1.3.6.1.6.3.1.1.4.1", "snmpTrapOID" },
        { "1.3.6.1.6.3.1.1.5.1", "coldStart" },
        { "1.3.6.1.6.3.1.1.5.2", "warmStart" },
        { "1.3.6.1.6.3.1.1.5.3", "linkDown" },
        { "1.3.6.1.6.3.1.1.5.4", "linkUp" },
        { "1.3.6.1.6.3.1.1.5.5", "authenticationFailure" },
        { "1.3.6.1.4.1.9.9.43.2.0.1", "ciscoConfigManEvent" },
        { "1.3.6.1.4.1.9.9.41.2.0.1", "clogMessageGenerated" },
        { "1.3.6.1.4.1.9", "cisco" }
    };

    // Obecné typy trapů SNMPv1 (pole generic-trap)
    static readonly string[] GenericTraps =
    {
        "coldStart", "warmStart", "linkDown", "linkUp", "authenticationFailure", "egpNeighborLoss", "enterpriseSpecific"
    };

    static int received = 0;  // Počet úspěšně zpracovaných trapů

    static async Task Main(string[] args)  // Hlavní asynchronní vstupní bod programu
    {
        // Kontrola počtu argumentů - port je volitelný
        if (args.Length > 1)
        {
            Console.WriteLine("Použití: SnmpTrapReceiver [port=162]");
            Console.WriteLine("Příklad: SnmpTrapReceiver 162");
            return;
        }

        int port = 162;  // Standardní port pro SNMP trapy
        if (args.Length == 1 && (!int.TryParse(args[0], out port) || port < 1 || port > 65535))
        {
            Console.WriteLine("Chyba: port musí být číslo 1-65535.");
            return;
        }

        using (CancellationTokenSource cts = new CancellationTokenSource())
        {
            // Ctrl+C ukončí smyčku příjmu
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                using (UdpClient listener = new UdpClient(port))
                {
                    Console.WriteLine($"SNMP Trap Receiver naslouchá na UDP portu {port}");
                    Console.WriteLine("Podporováno: SNMPv1 Trap, SNMPv2c Trap a Inform (potvrzení Informu se neodesílá)");
                    Console.WriteLine("Stiskněte Ctrl+C pro ukončení.\n");

                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            UdpReceiveResult result = await listener.ReceiveAsync(cts.Token);
                            HandleDatagram(result.Buffer, result.RemoteEndPoint);
                        }
                        catch (OperationCanceledException)
                        {
                            break;  // Ukončení na žádost uživatele
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"Chyba při příjmu: {ex.Message}");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Nelze otevřít port {port}: {ex.Message}");
                return;
            }
        }

        Console.WriteLine($"\nPřijato trapů: {received}");
    }

    // Zpracuje jeden datagram; chybně zakódované pakety nesmí shodit program
    static void HandleDatagram(byte[] data, IPEndPoint remote)
    {
        try
        {
            ParseTrap(data, remote);
        }
        catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException || ex is ArgumentException)
        {
            Console.WriteLine($"Neplatný SNMP paket od {remote}: {ex.Message}\n");
        }
    }

    static void ParseTrap(byte[] data, IPEndPoint remote)
    {
        int index = 0;

        // Vnější SEQUENCE
        ReadTlv(data, ref index, out byte tag);
        if (tag != 0x30) throw new FormatException("paket nezačíná SEQUENCE");

        // Verze (0 = v1, 1 = v2c, 3 = v3)
        int len = ReadTlv(data, ref index, out tag);
        if (tag != 0x02) throw new FormatException("chybí verze SNMP");
        long version = ParseInteger(data, index, len);
        index += len;

        // Komunita
        len = ReadTlv(data, ref index, out tag);
        if (tag != 0x04)
        {
            if (version == 3)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] SNMPv3 zpráva od {remote.Address} - SNMPv3 tento program nepodporuje.\n");
                return;
            }
            throw new FormatException("chybí komunita");
        }
        string community = Encoding.ASCII.GetString(data, index, len);
        index += len;

        // PDU - typ určuje druh zprávy
        int pduLen = ReadTlv(data, ref index, out byte pduType);
        int pduEnd = index + pduLen;

        string versionName = version == 0 ? "v1" : version == 1 ? "v2c" : $"v{version + 1}";

        if (pduType == 0xA4 && version == 0)
        {
            PrintHeader(remote, versionName, community, "Trap");
            ParseV1Trap(data, ref index, pduEnd);
        }
        else if (pduType == 0xA7 || pduType == 0xA6)
        {
            PrintHeader(remote, versionName, community, pduType == 0xA7 ? "Trap" : "Inform");
            ParseV2Trap(data, ref index, pduEnd);
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Datagram od {remote.Address} s jiným typem PDU (0x{pduType:X2}) - ignoruji.\n");
            return;
        }

        received++;
        Console.WriteLine();
    }

    static void PrintHeader(IPEndPoint remote, string version, string community, string kind)
    {
        Console.WriteLine($"=== SNMP{version} {kind} od {remote.Address}:{remote.Port} | community: {community} | {DateTime.Now:HH:mm:ss} ===");
    }

    // SNMPv1 Trap-PDU: enterprise, agent-addr, generic-trap, specific-trap, time-stamp, vazby
    static void ParseV1Trap(byte[] data, ref int index, int pduEnd)
    {
        int len = ReadTlv(data, ref index, out byte tag);
        string enterprise = DecodeOid(data, index, len);
        index += len;

        len = ReadTlv(data, ref index, out tag);
        string agent = len == 4 ? new IPAddress(Slice(data, index, 4)).ToString() : "?";
        index += len;

        len = ReadTlv(data, ref index, out tag);
        int generic = (int)ParseInteger(data, index, len);
        index += len;

        len = ReadTlv(data, ref index, out tag);
        long specific = ParseInteger(data, index, len);
        index += len;

        len = ReadTlv(data, ref index, out tag);
        string uptime = FormatTimeTicks((ulong)ParseInteger(data, index, len, unsigned: true));
        index += len;

        string genericName = generic >= 0 && generic < GenericTraps.Length ? GenericTraps[generic] : "neznámý";
        Console.WriteLine($"Typ trapu    : {genericName} ({generic}), specific: {specific}");
        Console.WriteLine($"Enterprise   : {DescribeOid(enterprise)}");
        Console.WriteLine($"Agent        : {agent}");
        Console.WriteLine($"Doba provozu : {uptime}");

        ParseVarbinds(data, ref index, pduEnd);
    }

    // SNMPv2c Trap/Inform: request-id, error-status, error-index, vazby (sysUpTime.0 a snmpTrapOID.0 jsou první dvě)
    static void ParseV2Trap(byte[] data, ref int index, int pduEnd)
    {
        for (int i = 0; i < 3; i++)  // Request ID, error status a error index se přeskočí
        {
            int len = ReadTlv(data, ref index, out _);
            index += len;
        }

        ParseVarbinds(data, ref index, pduEnd);
    }

    // Seznam vazeb (SEQUENCE OF SEQUENCE { OID, hodnota }) - vypíše každou vazbu na jeden řádek
    static void ParseVarbinds(byte[] data, ref int index, int pduEnd)
    {
        ReadTlv(data, ref index, out byte tag);
        if (tag != 0x30) throw new FormatException("chybí seznam vazeb");

        Console.WriteLine("Vazby:");
        while (index < pduEnd)
        {
            int bindLen = ReadTlv(data, ref index, out tag);
            if (tag != 0x30) throw new FormatException("neplatná vazba");
            int bindEnd = index + bindLen;

            int oidLen = ReadTlv(data, ref index, out tag);
            if (tag != 0x06) throw new FormatException("vazba nezačíná OID");
            string oid = DecodeOid(data, index, oidLen);
            index += oidLen;

            string value = ReadValue(data, ref index, oid);
            Console.WriteLine($"  {DescribeOid(oid)} = {value}");

            index = bindEnd;
        }
    }

    // Přečte hodnotu vazby podle jejího BER typu a posune index za ni
    static string ReadValue(byte[] data, ref int index, string oid)
    {
        int len = ReadTlv(data, ref index, out byte tag);
        string value;

        switch (tag)
        {
            case 0x02:  // INTEGER
                {
                    long number = ParseInteger(data, index, len);
                    value = number.ToString();
                    // Stav rozhraní je číselný kód - doplníme jeho význam
                    if (oid.StartsWith("1.3.6.1.2.1.2.2.1.7.") || oid.StartsWith("1.3.6.1.2.1.2.2.1.8."))
                        value += number == 1 ? " (up)" : number == 2 ? " (down)" : number == 3 ? " (testing)" : "";
                    break;
                }
            case 0x04:  // OCTET STRING
                value = FormatOctets(data, index, len);
                break;
            case 0x05:  // NULL
                value = "null";
                break;
            case 0x06:  // OBJECT IDENTIFIER
                value = DescribeOid(DecodeOid(data, index, len));
                break;
            case 0x40:  // IpAddress
                value = len == 4 ? new IPAddress(Slice(data, index, 4)).ToString() : FormatHex(data, index, len);
                break;
            case 0x41:  // Counter32
            case 0x42:  // Gauge32
            case 0x46:  // Counter64
                value = ParseInteger(data, index, len, unsigned: true).ToString();
                break;
            case 0x43:  // TimeTicks
                value = FormatTimeTicks((ulong)ParseInteger(data, index, len, unsigned: true));
                break;
            case 0x44:  // Opaque
                value = FormatHex(data, index, len);
                break;
            default:
                value = $"[typ 0x{tag:X2}] {FormatHex(data, index, len)}";
                break;
        }

        index += len;
        return value;
    }

    // Přečte BER hlavičku (tag + délka, včetně dlouhé formy) a posune index na začátek obsahu
    static int ReadTlv(byte[] data, ref int index, out byte tag)
    {
        tag = data[index++];
        int len = data[index++];
        if ((len & 0x80) != 0)
        {
            int count = len & 0x7F;
            if (count > 4) throw new FormatException("příliš dlouhé pole délky");
            len = 0;
            for (int i = 0; i < count; i++)
                len = (len << 8) | data[index++];
        }
        if (len < 0 || index + len > data.Length)
            throw new FormatException("neplatná délka v SNMP paketu");
        return len;
    }

    // Celé číslo v big-endian; se znaménkem (INTEGER) nebo bez (Counter, Gauge, TimeTicks)
    static long ParseInteger(byte[] data, int index, int len, bool unsigned = false)
    {
        if (len == 0) return 0;
        long value = (!unsigned && (data[index] & 0x80) != 0) ? -1 : 0;  // Rozšíření znaménka
        for (int i = 0; i < len; i++)
            value = (value << 8) | (long)(uint)data[index + i];
        return value;
    }

    // Dekóduje OID: první bajt kóduje dva členy (40*a + b), další členy jsou po 7 bitech s příznakem pokračování
    static string DecodeOid(byte[] data, int index, int len)
    {
        if (len == 0) return "";

        List<ulong> parts = new List<ulong>();
        ulong current = 0;
        for (int i = 0; i < len; i++)
        {
            byte b = data[index + i];
            current = (current << 7) | (ulong)(b & 0x7Fu);
            if ((b & 0x80) == 0)
            {
                parts.Add(current);
                current = 0;
            }
        }

        // První člen v poli zahrnuje dva členy OID
        ulong first = parts[0];
        StringBuilder sb = new StringBuilder();
        if (first < 40) sb.Append($"0.{first}");
        else if (first < 80) sb.Append($"1.{first - 40}");
        else sb.Append($"2.{first - 80}");

        for (int i = 1; i < parts.Count; i++)
            sb.Append('.').Append(parts[i]);

        return sb.ToString();
    }

    // OID doplněné o jméno, pokud je známé: "1.3.6.1.2.1.2.2.1.8.3 (ifOperStatus.3)"
    static string DescribeOid(string oid)
    {
        string bestKey = null;
        foreach (string key in KnownOids.Keys)
        {
            if ((oid == key || oid.StartsWith(key + ".")) && (bestKey == null || key.Length > bestKey.Length))
                bestKey = key;  // Vyhrává nejdelší shoda (nejpřesnější kmen)
        }

        if (bestKey == null) return oid;
        return $"{oid} ({KnownOids[bestKey]}{oid.Substring(bestKey.Length)})";
    }

    // TimeTicks jsou setiny sekundy - převod na dny, hodiny, minuty a sekundy
    static string FormatTimeTicks(ulong ticks)
    {
        TimeSpan span = TimeSpan.FromSeconds(ticks / 100.0);
        return $"{(int)span.TotalDays} d {span.Hours:00}:{span.Minutes:00}:{span.Seconds:00} ({ticks} ticks)";
    }

    // OCTET STRING: tisknutelný text zobrazíme jako text, jinak jako hexadecimální bajty (např. MAC adresa)
    static string FormatOctets(byte[] data, int index, int len)
    {
        for (int i = 0; i < len; i++)
        {
            byte b = data[index + i];
            if (b < 32 && b != 9 && b != 10 && b != 13 || b > 126)
                return FormatHex(data, index, len);
        }
        return "\"" + Encoding.ASCII.GetString(data, index, len) + "\"";
    }

    static string FormatHex(byte[] data, int index, int len)
    {
        return len == 0 ? "(prázdné)" : BitConverter.ToString(data, index, len).Replace('-', ':');
    }

    static byte[] Slice(byte[] data, int index, int len)
    {
        byte[] result = new byte[len];
        Array.Copy(data, index, result, 0, len);
        return result;
    }
}

/*
Struktura programu:
 Aplikace je přijímač SNMP trapů: naslouchá na UDP portu 162, kam zařízení posílají oznámení o událostech (výpadek linky, restart, změna konfigurace)
 Je protějškem programu SnmpWalker - ten se zařízení dotazuje, tento program čeká na zprávy odeslané zařízením
 Na Cisco zařízení se odesílání zapíná např. "snmp-server enable traps" a "snmp-server host <IP adresa tohoto počítače> version 2c <community>"
Klíčové komponenty:
 Main() - kontrola parametrů, smyčka příjmu a ukončení přes Ctrl+C
 ParseTrap() - rozpoznání verze, komunity a typu PDU
 ParseV1Trap() / ParseV2Trap() - zpracování obou formátů trapu
 ParseVarbinds() / ReadValue() - výpis vazeb (OID a hodnota) podle BER typu
 DecodeOid() / DescribeOid() - převod OID a doplnění jmen známých objektů (linkDown, ifOperStatus, ...)
Formát SNMP (BER):
 Každý údaj má tvar tag-délka-hodnota; SEQUENCE (0x30) obsahuje další údaje
 PDU 0xA4 = Trap SNMPv1, 0xA7 = Trap SNMPv2c, 0xA6 = InformRequest
Omezení:
 SNMPv3 (šifrované zprávy) se nedekóduje; potvrzení Informu (Response) se neodesílá, odesílatel ho proto může opakovat
 Na portu 162 musí být povolený příchozí provoz ve firewallu
Výstupy:
 Verze, adresa odesílatele, community, typ trapu a seznam vazeb s názvy známých OID
*/
