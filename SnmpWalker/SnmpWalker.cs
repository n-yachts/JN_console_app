using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class SnmpWalker
{
    static async Task Main(string[] args)
    {
        // Kontrola počtu argumentů
        if (args.Length != 4)
        {
            Console.WriteLine("Použití: SnmpWalker <host> <community> <startOID> <timeout>");
            Console.WriteLine("Příklad: SnmpWalker 192.168.1.1 public 1.3.6.1.2.1.1 5000");
            return;
        }

        // Parsování argumentů
        string host = args[0];           // Cílové zařízení
        string community = args[1];      // SNMP komunita (např. "public")
        string startOID = args[2];       // Počáteční OID pro procházení
        int timeout = int.Parse(args[3]);// Timeout v milisekundách

        try
        {
            Console.WriteLine($"SNMP Walk pro {host} začínající na OID {startOID}\n");

            // Provedení SNMP Walk operace
            var results = await SnmpWalk(host, community, startOID, timeout);

            // Výpis všech získaných hodnot
            foreach (var result in results)
            {
                Console.WriteLine($"{result.Key} = {result.Value}");
            }

            Console.WriteLine($"\nCelkem nalezeno {results.Count} OIDů");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static async Task<Dictionary<string, string>> SnmpWalk(string host, string community, string startOID, int timeout)
    {
        var results = new Dictionary<string, string>();
        string currentOID = startOID;  // Začneme zadaným OID

        // Vytvoření UDP klienta pro komunikaci
        using (UdpClient client = new UdpClient())
        {
            // Nastavení timeoutu pro přijímací operace
            client.Client.ReceiveTimeout = timeout;
            // Cílová adresa a port (SNMP standardně používá port 161)
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(host), 161);

            // Hlavní smyčka pro procházení SNMP stromu
            while (true)
            {
                try
                {
                    // Vytvoření GetNextRequest SNMP paketu
                    byte[] request = CreateGetNextRequest(community, currentOID);
                    // Odeslání požadavku
                    await client.SendAsync(request, request.Length, endpoint);

                    // Přijetí odpovědi
                    // (ReceiveTimeout se na asynchronní ReceiveAsync neuplatňuje, timeout řešíme ručně)
                    Task<UdpReceiveResult> receiveTask = client.ReceiveAsync();
                    if (await Task.WhenAny(receiveTask, Task.Delay(timeout)) != receiveTask)
                        throw new TimeoutException();
                    byte[] responseData = receiveTask.Result.Buffer;

                    // Zpracování odpovědi a získání dalšího OID
                    string nextOID = ParseGetNextResponse(responseData, out string value);

                    // Kontrola konce stromu (OID již nepatří do požadované větve)
                    if (nextOID == null || (nextOID != startOID && !nextOID.StartsWith(startOID + ".")))
                        break;

                    // Detekce cyklu (ochrana proti nekonečné smyčce)
                    if (results.ContainsKey(nextOID))
                        break;

                    // Uložení výsledku a posun na další OID
                    results[nextOID] = value;
                    currentOID = nextOID;

                    Console.Write(".");  // Indikace průběhu
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("\nTimeout při komunikaci se zařízením");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nChyba: {ex.Message}");
                    break;
                }
            }
        }

        return results;
    }

    static byte[] CreateGetNextRequest(string community, string oid)
    {
        List<byte> packet = new List<byte>();

        // SNMP verze (0x01 = SNMPv2c)
        byte[] version = new byte[] { 0x02, 0x01, 0x01 };

        // Kódování komunity
        byte[] communityBytes = Encoding.ASCII.GetBytes(community);
        byte[] communityField = new byte[communityBytes.Length + 2];
        communityField[0] = 0x04;  // Typ: OCTET STRING
        communityField[1] = (byte)communityBytes.Length;  // Délka
        Array.Copy(communityBytes, 0, communityField, 2, communityBytes.Length);

        // Vytvoření PDU pro GetNextRequest
        byte[] pdu = CreateGetNextPdu(oid);

        // Výpočet celkové délky paketu
        int totalLength = version.Length + communityField.Length + pdu.Length;

        // Hlavní sekvence SNMP zprávy
        packet.Add(0x30);  // SEQUENCE
        packet.Add((byte)totalLength);  // Délka celé zprávy
        packet.AddRange(version);
        packet.AddRange(communityField);
        packet.AddRange(pdu);

        return packet.ToArray();
    }

    static byte[] CreateGetNextPdu(string oid)
    {
        List<byte> pdu = new List<byte>();

        // Request ID (základní hodnota)
        byte[] requestId = new byte[] { 0x02, 0x01, 0x01 };

        // Error status (0 = noError)
        byte[] errorStatus = new byte[] { 0x02, 0x01, 0x00 };

        // Error index (0 = žádná chyba)
        byte[] errorIndex = new byte[] { 0x02, 0x01, 0x00 };

        // Vytvoření vazby proměnných (variable binding)
        byte[] varbind = CreateVarbind(oid);

        // Výpočet délky PDU
        // (+2 bajty za hlavičku SEQUENCE seznamu vazeb)
        int pduLength = requestId.Length + errorStatus.Length + errorIndex.Length + 2 + varbind.Length;

        // Sestavení PDU
        pdu.Add(0xA1);  // GetNextRequest PDU typ
        pdu.Add((byte)pduLength);  // Délka PDU
        pdu.AddRange(requestId);
        pdu.AddRange(errorStatus);
        pdu.AddRange(errorIndex);
        pdu.Add(0x30);  // SEQUENCE pro seznam vazeb
        pdu.Add((byte)varbind.Length);  // Délka obsahu (jedna vazba včetně své hlavičky)
        pdu.AddRange(varbind);

        return pdu.ToArray();
    }

    static byte[] CreateVarbind(string oid)
    {
        List<byte> varbind = new List<byte>();

        // Kódování OID
        byte[] oidBytes = EncodeOid(oid);
        byte[] oidField = new byte[oidBytes.Length + 2];
        oidField[0] = 0x06;  // Typ: OBJECT IDENTIFIER
        oidField[1] = (byte)oidBytes.Length;  // Délka OID
        Array.Copy(oidBytes, 0, oidField, 2, oidBytes.Length);

        // Hodnota pro GetNextRequest je vždy null
        byte[] nullValue = new byte[] { 0x05, 0x00 };  // NULL hodnota

        // Sekvence pro vazbu (OID + hodnota)
        varbind.Add(0x30);  // SEQUENCE
        varbind.Add((byte)(oidField.Length + nullValue.Length));  // Celková délka
        varbind.AddRange(oidField);
        varbind.AddRange(nullValue);

        return varbind.ToArray();
    }

    static byte[] EncodeOid(string oid)
    {
        string[] parts = oid.Split('.');
        List<byte> result = new List<byte>();

        // Speciální kódování prvních dvou členů OID
        if (parts.Length >= 2)
        {
            int first = int.Parse(parts[0]);
            int second = int.Parse(parts[1]);
            result.Add((byte)(first * 40 + second));  // Pravidlo kódování
        }

        // Kódování zbylých členů OID
        for (int i = 2; i < parts.Length; i++)
        {
            int value = int.Parse(parts[i]);
            if (value < 128)
            {
                // Základní kódování pro malé hodnoty
                result.Add((byte)value);
            }
            else
            {
                // Vícebajtové kódování pro větší hodnoty
                List<byte> temp = new List<byte>();
                while (value > 0)
                {
                    temp.Insert(0, (byte)(value & 0x7F));  // Uložení 7 bitů
                    value >>= 7;  // Posun o 7 bitů
                }
                // Nastavení MSB u všech bajtů kromě posledního
                for (int j = 0; j < temp.Count - 1; j++)
                    temp[j] |= 0x80;
                result.AddRange(temp);
            }
        }

        return result.ToArray();
    }


    // Přečte BER hlavičku (tag + délka, včetně dlouhé formy) a posune index na začátek obsahu
    static int ReadTlv(byte[] data, ref int index, out byte tag)
    {
        tag = data[index++];
        int len = data[index++];
        if ((len & 0x80) != 0)
        {
            int count = len & 0x7F;
            len = 0;
            for (int i = 0; i < count; i++)
                len = (len << 8) | data[index++];
        }
        if (len < 0 || index + len > data.Length)
            throw new FormatException("Neplatná délka v SNMP odpovědi");
        return len;
    }

    static string ParseGetNextResponse(byte[] data, out string value)
    {
        value = null;
        string oid = null;

        try
        {
            int index = 0;

            // Vnější SEQUENCE
            ReadTlv(data, ref index, out byte tag);
            if (tag != 0x30) return null;

            // Verze a komunita (přeskočit)
            for (int i = 0; i < 2; i++)
            {
                int len = ReadTlv(data, ref index, out _);
                index += len;
            }

            // PDU (GetResponse = 0xA2)
            ReadTlv(data, ref index, out tag);
            if (tag != 0xA2) return null;

            // Request ID, error status, error index
            int errorStatus = 0;
            for (int i = 0; i < 3; i++)
            {
                int len = ReadTlv(data, ref index, out _);
                if (i == 1) errorStatus = int.Parse(ParseIntegerValue(data, index, len));
                index += len;
            }
            if (errorStatus != 0) return null;

            // Seznam vazeb a první vazba
            ReadTlv(data, ref index, out tag);
            if (tag != 0x30) return null;
            ReadTlv(data, ref index, out tag);
            if (tag != 0x30) return null;

            // OID
            int oidLen = ReadTlv(data, ref index, out tag);
            if (tag != 0x06) return null;
            oid = DecodeOid(data, index, oidLen);
            index += oidLen;

            // Hodnota
            int valueLen = ReadTlv(data, ref index, out byte valueType);
            switch (valueType)
            {
                case 0x04:
                    value = Encoding.ASCII.GetString(data, index, valueLen);
                    break;
                case 0x02:  // INTEGER
                case 0x41:  // Counter32
                case 0x42:  // Gauge32
                case 0x43:  // TimeTicks
                case 0x46:  // Counter64
                    value = ParseIntegerValue(data, index, valueLen);
                    break;
                case 0x06:
                    value = DecodeOid(data, index, valueLen);
                    break;
                case 0x05:
                    value = "null";
                    break;
                case 0x81:  // noSuchInstance
                case 0x82:  // endOfMibView
                    return null;  // Konec stromu
                default:
                    value = $"[Type: 0x{valueType:X2}]";
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při parsování: {ex.Message}");
            return null;
        }

        return oid;
    }

    static string ParseIntegerValue(byte[] data, int index, int length)
    {
        // Zpracování prázdné hodnoty
        if (length == 0) return "0";

        long result = 0;

        // Detekce znaménka (první bit prvního bajtu)
        bool isNegative = (data[index] & 0x80) != 0;

        if (isNegative)
        {
            // Inicializace záporného čísla (doplněk jedničkami)
            for (int i = 0; i < sizeof(long); i++)
            {
                result = (result << 8) | 0xFF;
            }
        }

        // Skládání hodnoty z jednotlivých bajtů
        for (int i = 0; i < length; i++)
        {
            result = (result << 8) | data[index + i];
        }

        return result.ToString();
    }

    static string DecodeOid(byte[] data, int index, int length)
    {
        List<string> parts = new List<string>();

        // Zpracování prvního bajtu (speciální pravidlo)
        if (length > 0)
        {
            int first = data[index];
            parts.Add((first / 40).ToString());  // První člen
            parts.Add((first % 40).ToString());  // Druhý člen
        }

        // Zpracování zbylých bajtů
        for (int i = 1; i < length; i++)
        {
            int value = 0;
            // Čtení vícebajtové hodnoty (MSB = 1 znamená pokračování)
            while (i < length && (data[index + i] & 0x80) != 0)
            {
                value = (value << 7) | (data[index + i] & 0x7F);  // Skládání hodnoty
                i++;
            }
            if (i < length)
            {
                value = (value << 7) | (data[index + i] & 0x7F);  // Finální bajt
                parts.Add(value.ToString());
            }
        }

        return string.Join(".", parts);
    }
}

/*
Asynchronní operace - využívá async/await pro neblokující síťovou komunikaci
SNMPv2c protokol - implementuje GetNextRequest operace
Průchod MIB stromem - pomocí iterativních GetNext požadavků
Základní error handling - timeout (řešený ručně přes Task.WhenAny, protože ReceiveAsync ignoruje ReceiveTimeout) a detekce chyb
Podpora základních datových typů - OID, řetězce, čísla
Kódování BER (TLV: typ, délka, hodnota) - pro SNMP zprávy; délky nad 127 bajtů používají dlouhou formu

OID (Object Identifier) - Objektový identifikátor
OID je hierarchický systém jednoznačného označování objektů v různých systémech,
    nejčastěji používaný v SNMP (Simple Network Management Protocol) pro identifikaci spravovaných objektů v síťových zařízeních.
    (další informace si najděte na internetu)
*/