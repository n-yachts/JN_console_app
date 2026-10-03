using System;
using System.Net.Sockets;  // TcpClient a NetworkStream pro komunikaci přes TCP
using System.Threading.Tasks;  // Asynchronní programování (Task, async/await)

// Hlavní třída pro skenování Modbus zařízení
class ModbusScanner
{
    // Hlavní asynchronní vstupní bod programu
    static async Task Main(string[] args)
    {
        // Kontrola počtu argumentů (očekáváme IP adresu/hostname)
        if (args.Length != 1)
        {
            Console.WriteLine("Použití: ModbusScanner <host>");
            Console.WriteLine("Příklad: ModbusScanner 192.168.1.100");
            return; // Ukončení programu při chybném počtu argumentů
        }

        string host = args[0]; // Získání IP adresy z argumentů
        int port = 502; // Standardní port pro Modbus TCP

        Console.WriteLine($"Skenování Modbus zařízení na {host}:{port}...\n");

        // Spuštění hlavní skenovací metody
        await ScanModbusFunctions(host, port);
    }

    // Metoda pro testování různých Modbus funkcí
    static async Task ScanModbusFunctions(string host, int port)
    {
        using (TcpClient client = new TcpClient()) // Využití using pro automatické uvolnění prostředků
        {
            try
            {
                // Pokus o připojení k zařízení
                await client.ConnectAsync(host, port);
                Console.WriteLine("✅ Úspěšně připojeno k Modbus zařízení\n");

                // Seznam testovaných Modbus funkčních kódů
                // Testují se pouze čtecí funkce - zápisové funkce (5, 6, 15, 16) by na skutečném
                // zařízení (PLC) změnily stav výstupů/registrů, proto se neodesílají
                byte[] functionsToTest = { 1, 2, 3, 4 };

                // Iterace přes všechny funkční kódy
                foreach (byte functionCode in functionsToTest)
                {
                    await TestModbusFunction(client, functionCode); // Test jednotlivé funkce
                    await Task.Delay(100); // Bezpečnostní pauza mezi požadavky
                }
            }
            catch (Exception ex)
            {
                // Zachycení chyb připojení
                Console.WriteLine($"❌ Nelze připojit k zařízení: {ex.Message}");
            }
        } // Automatické uvolnění TcpClient připojení
    }

    // Metoda pro testování konkrétní Modbus funkce
    static async Task TestModbusFunction(TcpClient client, byte functionCode)
    {
        try
        {
            // Vytvoření Modbus požadavku pro danou funkci
            byte[] request = CreateModbusRequest(functionCode);
            NetworkStream stream = client.GetStream(); // Získání síťového streamu

            // Odeslání požadavku do zařízení
            await stream.WriteAsync(request, 0, request.Length);

            // Příprava bufferu pro odpověď
            byte[] responseBuffer = new byte[256];
            Task<int> readTask = stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            if (await Task.WhenAny(readTask, Task.Delay(3000)) != readTask)
                throw new TimeoutException("Zařízení neodpovědělo");
            int bytesRead = readTask.Result;

            string functionName = GetFunctionName(functionCode);

            if (bytesRead < 8)
            {
                Console.WriteLine($"❌ Funkce {functionCode} ({functionName}): neplatná odpověď");
            }
            // Exception response: funkční kód má nastavený nejvyšší bit, kód výjimky je v dalším bajtu
            else if ((responseBuffer[7] & 0x80) != 0)
            {
                byte exceptionCode = bytesRead > 8 ? responseBuffer[8] : (byte)0;
                if (exceptionCode == 0x01) // Illegal Function
                    Console.WriteLine($"❌ Funkce {functionCode} ({functionName}): NEPODPOROVÁNO (Illegal Function)");
                else
                    Console.WriteLine($"✅ Funkce {functionCode} ({functionName}): PODPOROVÁNO (výjimka {exceptionCode} - např. neplatná adresa)");
            }
            else
            {
                Console.WriteLine($"✅ Funkce {functionCode} ({functionName}): PODPOROVÁNO");

                // Speciální zpracování pro čtecí funkce registrů
                if (functionCode == 3 || functionCode == 4)
                {
                    ParseHoldingRegisters(responseBuffer, bytesRead);
                }
            }
        }
        catch (Exception ex)
        {
            // Zachycení chyb při komunikaci pro konkrétní funkci
            string functionName = GetFunctionName(functionCode);
            Console.WriteLine($"❌ Funkce {functionCode} ({functionName}): NEPODPOROVÁNO - {ex.Message}");
        }
    }

    // Metoda pro vytvoření platného Modbus TCP rámce
    static byte[] CreateModbusRequest(byte functionCode)
    {
        byte[] request = new byte[12]; // Standardní velikost pro základní požadavek
        Random random = new Random();

        // MBAP Header (Modbus Application Protocol)
        ushort transactionId = (ushort)random.Next(); // Náhodné ID transakce
        request[0] = (byte)(transactionId >> 8);  // High byte
        request[1] = (byte)transactionId;         // Low byte

        request[2] = 0x00; // Protokol ID (vždy 0 pro Modbus)
        request[3] = 0x00;

        request[4] = 0x00; // Délka zprávy (high byte)
        request[5] = 0x06; // Délka zprávy (low byte) - 6 následujících bytů

        request[6] = 0x01; // Unit ID (identifikace zařízení na sběrnici)

        // PDU (Protocol Data Unit) - vlastní data požadavku
        request[7] = functionCode;  // Funkční kód
        request[8] = 0x00;         // Počáteční adresa (high byte)
        request[9] = 0x00;         // Počáteční adresa (low byte) - registr 0
        request[10] = 0x00;        // Počet (high byte)
        request[11] = 0x01;        // Počet (low byte) - čte 1 registr/coil

        return request;
    }

    // Pomocná metoda pro získání názvu funkce z kódu
    static string GetFunctionName(byte functionCode)
    {
        return functionCode switch
        {
            1 => "Read Coils",                   // Čtení výstupních bitů (koilů)
            2 => "Read Discrete Inputs",         // Čtení vstupních bitů
            3 => "Read Holding Registers",       // Čtení výstupních registrů
            4 => "Read Input Registers",         // Čtení vstupních registrů
            5 => "Write Single Coil",            // Zápis jednoho bitu
            6 => "Write Single Register",        // Zápis jednoho registru
            15 => "Write Multiple Coils",        // Zápis více bitů
            16 => "Write Multiple Registers",    // Zápis více registrů
            _ => "Unknown"                       // Neznámý funkční kód
        };
    }

    // Metoda pro parsování hodnot z odpovědi pro čtení registrů
    static void ParseHoldingRegisters(byte[] response, int length)
    {
        // Kontrola platnosti odpovědi (správný funkční kód a minimální délka)
        if (length >= 11 && (response[7] == 0x03 || response[7] == 0x04)) // 0x03 = Holding, 0x04 = Input Registers
        {
            byte byteCount = response[8]; // Počet bytů s daty
            if (byteCount >= 2) // Minimálně 2 byty pro jeden registr
            {
                // Složení 16-bitové hodnoty registru z vysokého a nízkého bytu
                ushort registerValue = (ushort)((response[9] << 8) | response[10]);
                Console.WriteLine($"   Hodnota registru: {registerValue} (0x{registerValue:X4})");
            }
        }
    }
}

/*
Modbus TCP Komunikace - Používá standardní TCP socket pro komunikaci
Testování Funkcí - Testuje 4 čtecí Modbus funkce (zápisové funkce se záměrně neodesílají)
Chybové Zpracování - Zachycení výjimek a rozlišení exception response (odpověď s nejvyšším bitem funkčního kódu; kód 1 = Illegal Function znamená nepodporovanou funkci)
MBAP Hlavička - Správná tvorba Modbus Application Protocol hlavičky
Čitelný Výstup - Přehledné zobrazení výsledků s emoji pro rychlou orientaci

Kód je vhodný pro základní detekci Modbus zařízení a testování podporovaných funkcí v síti.
*/