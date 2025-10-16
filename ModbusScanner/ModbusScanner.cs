using System;
using System.Net.Sockets;
using System.Threading.Tasks;

class ModbusScanner
{
    static async Task Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: ModbusScanner <host>");
            Console.WriteLine("Příklad: ModbusScanner 192.168.1.100");
            return;
        }

        string host = args[0];
        int port = 502;

        Console.WriteLine($"Skenování Modbus zařízení na {host}:{port}...\n");

        await ScanModbusFunctions(host, port);
    }

    static async Task ScanModbusFunctions(string host, int port)
    {
        using (TcpClient client = new TcpClient())
        {
            try
            {
                await client.ConnectAsync(host, port);
                Console.WriteLine("✅ Úspěšně připojeno k Modbus zařízení\n");

                // Testování různých funkcí
                byte[] functionsToTest = { 1, 2, 3, 4, 5, 6, 15, 16 };

                foreach (byte functionCode in functionsToTest)
                {
                    await TestModbusFunction(client, functionCode);
                    await Task.Delay(100); // Krátká pauza mezi požadavky
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Nelze připojit k zařízení: {ex.Message}");
            }
        }
    }

    static async Task TestModbusFunction(TcpClient client, byte functionCode)
    {
        try
        {
            byte[] request = CreateModbusRequest(functionCode);
            NetworkStream stream = client.GetStream();

            await stream.WriteAsync(request, 0, request.Length);

            // Čtení odpovědi
            byte[] responseBuffer = new byte[256];
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);

            if (bytesRead > 0)
            {
                string functionName = GetFunctionName(functionCode);
                Console.WriteLine($"✅ Funkce {functionCode} ({functionName}): PODPOROVÁNO");

                if (functionCode == 3 || functionCode == 4)
                {
                    ParseHoldingRegisters(responseBuffer, bytesRead);
                }
            }
        }
        catch (Exception ex)
        {
            string functionName = GetFunctionName(functionCode);
            Console.WriteLine($"❌ Funkce {functionCode} ({functionName}): NEPODPOROVÁNO - {ex.Message}");
        }
    }

    static byte[] CreateModbusRequest(byte functionCode)
    {
        byte[] request = new byte[12];
        Random random = new Random();

        // MBAP Header
        ushort transactionId = (ushort)random.Next();
        request[0] = (byte)(transactionId >> 8);
        request[1] = (byte)transactionId;

        request[2] = 0x00; // Protocol ID
        request[3] = 0x00;

        request[4] = 0x00; // Length (will be set)
        request[5] = 0x06; // Unit ID + Function Code + Address + Quantity

        request[6] = 0x01; // Unit ID

        // PDU
        request[7] = functionCode; // Function Code
        request[8] = 0x00; // Starting Address High
        request[9] = 0x00; // Starting Address Low
        request[10] = 0x00; // Quantity High
        request[11] = 0x01; // Quantity Low (read 1 register/coil)

        return request;
    }

    static string GetFunctionName(byte functionCode)
    {
        return functionCode switch
        {
            1 => "Read Coils",
            2 => "Read Discrete Inputs",
            3 => "Read Holding Registers",
            4 => "Read Input Registers",
            5 => "Write Single Coil",
            6 => "Write Single Register",
            15 => "Write Multiple Coils",
            16 => "Write Multiple Registers",
            _ => "Unknown"
        };
    }

    static void ParseHoldingRegisters(byte[] response, int length)
    {
        if (length >= 9 && response[7] == 0x03) // Read Holding Registers response
        {
            byte byteCount = response[8];
            if (byteCount >= 2)
            {
                ushort registerValue = (ushort)((response[9] << 8) | response[10]);
                Console.WriteLine($"   Hodnota registru: {registerValue} (0x{registerValue:X4})");
            }
        }
    }
}