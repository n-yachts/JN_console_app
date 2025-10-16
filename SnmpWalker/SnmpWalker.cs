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
        if (args.Length != 4)
        {
            Console.WriteLine("Použití: SnmpWalker <host> <community> <startOID> <timeout>");
            Console.WriteLine("Příklad: SnmpWalker 192.168.1.1 public 1.3.6.1.2.1.1 5000");
            return;
        }

        string host = args[0];
        string community = args[1];
        string startOID = args[2];
        int timeout = int.Parse(args[3]);

        try
        {
            Console.WriteLine($"SNMP Walk pro {host} začínající na OID {startOID}\n");

            var results = await SnmpWalk(host, community, startOID, timeout);

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
        string currentOID = startOID;

        using (UdpClient client = new UdpClient())
        {
            client.Client.ReceiveTimeout = timeout;
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(host), 161);

            while (true)
            {
                try
                {
                    byte[] request = CreateGetNextRequest(community, currentOID);
                    await client.SendAsync(request, request.Length, endpoint);

                    UdpReceiveResult response = await client.ReceiveAsync();
                    byte[] responseData = response.Buffer;

                    string nextOID = ParseGetNextResponse(responseData, out string value);

                    if (nextOID == null || !nextOID.StartsWith(startOID))
                        break;

                    if (results.ContainsKey(nextOID))
                        break; // Detekce cyklu

                    results[nextOID] = value;
                    currentOID = nextOID;

                    Console.Write(".");
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

        // SNMP Version (SNMPv2c)
        byte[] version = new byte[] { 0x02, 0x01, 0x01 };

        // Community string
        byte[] communityBytes = Encoding.ASCII.GetBytes(community);
        byte[] communityField = new byte[communityBytes.Length + 2];
        communityField[0] = 0x04;
        communityField[1] = (byte)communityBytes.Length;
        Array.Copy(communityBytes, 0, communityField, 2, communityBytes.Length);

        // GetNextRequest PDU
        byte[] pdu = CreateGetNextPdu(oid);

        // Celková délka
        int totalLength = version.Length + communityField.Length + pdu.Length;

        // SNMP Message sequence
        packet.Add(0x30); // SEQUENCE
        packet.Add((byte)totalLength);
        packet.AddRange(version);
        packet.AddRange(communityField);
        packet.AddRange(pdu);

        return packet.ToArray();
    }

    static byte[] CreateGetNextPdu(string oid)
    {
        List<byte> pdu = new List<byte>();

        // Request ID
        byte[] requestId = new byte[] { 0x02, 0x01, 0x01 };

        // Error status
        byte[] errorStatus = new byte[] { 0x02, 0x01, 0x00 };

        // Error index
        byte[] errorIndex = new byte[] { 0x02, 0x01, 0x00 };

        // Variable bindings
        byte[] varbind = CreateVarbind(oid);

        // PDU length
        int pduLength = requestId.Length + errorStatus.Length + errorIndex.Length + varbind.Length;

        // GetNextRequest PDU
        pdu.Add(0xA1); // GetNextRequest
        pdu.Add((byte)pduLength);
        pdu.AddRange(requestId);
        pdu.AddRange(errorStatus);
        pdu.AddRange(errorIndex);
        pdu.Add(0x30); // SEQUENCE for varbind list
        pdu.Add((byte)(varbind.Length - 2)); // Length without outer sequence
        pdu.AddRange(varbind);

        return pdu.ToArray();
    }

    static byte[] CreateVarbind(string oid)
    {
        List<byte> varbind = new List<byte>();

        // OID
        byte[] oidBytes = EncodeOid(oid);
        byte[] oidField = new byte[oidBytes.Length + 2];
        oidField[0] = 0x06; // OBJECT IDENTIFIER
        oidField[1] = (byte)oidBytes.Length;
        Array.Copy(oidBytes, 0, oidField, 2, oidBytes.Length);

        // Null value
        byte[] nullValue = new byte[] { 0x05, 0x00 }; // NULL

        // Varbind sequence
        varbind.Add(0x30); // SEQUENCE
        varbind.Add((byte)(oidField.Length + nullValue.Length));
        varbind.AddRange(oidField);
        varbind.AddRange(nullValue);

        return varbind.ToArray();
    }

    static byte[] EncodeOid(string oid)
    {
        string[] parts = oid.Split('.');
        List<byte> result = new List<byte>();

        // První dva členy se kódují speciálně
        if (parts.Length >= 2)
        {
            int first = int.Parse(parts[0]);
            int second = int.Parse(parts[1]);
            result.Add((byte)(first * 40 + second));
        }

        // Zbylé členy
        for (int i = 2; i < parts.Length; i++)
        {
            int value = int.Parse(parts[i]);
            if (value < 128)
            {
                result.Add((byte)value);
            }
            else
            {
                // Vícebajtové kódování
                List<byte> temp = new List<byte>();
                while (value > 0)
                {
                    temp.Insert(0, (byte)(value & 0x7F));
                    value >>= 7;
                }
                for (int j = 0; j < temp.Count - 1; j++)
                    temp[j] |= 0x80;
                result.AddRange(temp);
            }
        }

        return result.ToArray();
    }

    static string ParseGetNextResponse(byte[] data, out string value)
    {
        value = null;
        string oid = null;

        try
        {
            // Zjednodušené parsování - v reálném světě by bylo potřeba komplexnější řešení
            int index = 0;

            // Přeskočení hlavičky
            if (data[index++] == 0x30)
            {
                index++; // Délka
                // Přeskočení version a community
                while (index < data.Length && data[index] != 0xA2) index++;

                if (index < data.Length && data[index] == 0xA2) // GetResponse
                {
                    index++; // PDU type
                    index++; // PDU length

                    // Přeskočení request ID, error status, error index
                    for (int i = 0; i < 3; i++)
                    {
                        index++; // Type
                        int len = data[index++];
                        index += len;
                    }

                    // Variable bindings
                    if (index < data.Length && data[index] == 0x30)
                    {
                        index++; // Sequence
                        index++; // Length

                        // Varbind
                        if (index < data.Length && data[index] == 0x30)
                        {
                            index++; // Sequence
                            index++; // Length

                            // OID
                            if (index < data.Length && data[index] == 0x06)
                            {
                                index++; // OID type
                                int oidLen = data[index++];
                                oid = DecodeOid(data, index, oidLen);
                                index += oidLen;

                                // Value
                                if (index < data.Length)
                                {
                                    byte valueType = data[index++];
                                    int valueLen = data[index++];

                                    if (valueType == 0x04) // OCTET STRING
                                        value = Encoding.ASCII.GetString(data, index, valueLen);
                                    else if (valueType == 0x02) // INTEGER
                                        value = BitConverter.ToInt32(data, index).ToString();
                                    else if (valueType == 0x06) // OBJECT IDENTIFIER
                                        value = DecodeOid(data, index, valueLen);
                                    else
                                        value = $"[Type: {valueType}]";
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při parsování: {ex.Message}");
        }

        return oid;
    }

    static string DecodeOid(byte[] data, int index, int length)
    {
        List<string> parts = new List<string>();

        // První bajt
        if (length > 0)
        {
            int first = data[index];
            parts.Add((first / 40).ToString());
            parts.Add((first % 40).ToString());
        }

        // Zbylé bajty
        for (int i = 1; i < length; i++)
        {
            int value = 0;
            while (i < length && (data[index + i] & 0x80) != 0)
            {
                value = (value << 7) | (data[index + i] & 0x7F);
                i++;
            }
            if (i < length)
            {
                value = (value << 7) | (data[index + i] & 0x7F);
                parts.Add(value.ToString());
            }
        }

        return string.Join(".", parts);
    }
}