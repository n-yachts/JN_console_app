using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class RadiusClient
{
    static void Main(string[] args)
    {
        // Kontrola počtu argumentů
        if (args.Length != 4)
        {
            Console.WriteLine("Použití: RadiusClient <server> <secret> <username> <password>");
            Console.WriteLine("Příklad: RadiusClient 192.168.1.1 secret123 user1 pass123");
            return;
        }

        // Načtení parametrů z příkazové řádky
        string server = args[0];
        string secret = args[1];
        string username = args[2];
        string password = args[3];

        try
        {
            // Vytvoření UDP klienta pro komunikaci
            using (UdpClient client = new UdpClient())
            {
                // Nastavení cílového RADIUS serveru (standardní port 1812)
                IPEndPoint radiusEndpoint = new IPEndPoint(IPAddress.Parse(server), 1812);

                // Sestavení RADIUS požadavku
                byte[] requestPacket = CreateRadiusRequest(secret, username, password);

                Console.WriteLine($"Odesílám RADIUS request pro uživatele '{username}'");
                // Odeslání paketu přes UDP
                client.Send(requestPacket, requestPacket.Length, radiusEndpoint);

                // Nastavení timeoutu pro odpověď (5 sekund)
                client.Client.ReceiveTimeout = 5000;
                IPEndPoint responseEndpoint = new IPEndPoint(IPAddress.Any, 0);
                // Příjem odpovědi od serveru
                byte[] responseData = client.Receive(ref responseEndpoint);

                // Zpracování odpovědi
                ParseRadiusResponse(responseData, secret);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static byte[] CreateRadiusRequest(string secret, string username, string password)
    {
        Random random = new Random();
        byte[] packet = new byte[4096]; // Vyrovnávací paměť pro paket
        byte[] requestAuthenticator = new byte[16]; // 16-bytový náhodný authenticator
        random.NextBytes(requestAuthenticator); // Naplnění náhodnými daty

        // Hlavička RADIUS paketu
        packet[0] = 1; // Code: 1 = Access-Request
        packet[1] = (byte)random.Next(256); // Náhodný identifikátor relace
        packet[2] = 0; // Délka paketu (horní bajt - nastavíme později)
        packet[3] = 0; // Délka paketu (dolní bajt)

        // Kopírování authenticatoru do hlavičky
        Array.Copy(requestAuthenticator, 0, packet, 4, 16);

        int offset = 20; // Začátek za hlavičkou (20 bajtů)

        // Atribut User-Name (typ 1)
        byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
        packet[offset++] = 1; // Typ atributu
        packet[offset++] = (byte)(usernameBytes.Length + 2); // Délka atributu (hodnota + 2 bajty hlavičky)
        Array.Copy(usernameBytes, 0, packet, offset, usernameBytes.Length);
        offset += usernameBytes.Length;

        // Atribut User-Password (typ 2) - zašifrovaný pomocí MD5
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] encryptedPassword = EncryptRadiusPassword(passwordBytes, requestAuthenticator, secret);
        packet[offset++] = 2; // Typ atributu
        packet[offset++] = (byte)(encryptedPassword.Length + 2); // Délka atributu
        Array.Copy(encryptedPassword, 0, packet, offset, encryptedPassword.Length);
        offset += encryptedPassword.Length;

        // Atribut NAS-Identifier (typ 32) - identifikace klienta
        byte[] nasId = Encoding.UTF8.GetBytes("radius-client");
        packet[offset++] = 32; // Typ atributu
        packet[offset++] = (byte)(nasId.Length + 2); // Délka atributu
        Array.Copy(nasId, 0, packet, offset, nasId.Length);
        offset += nasId.Length;

        // Nastavení celkové délky paketu
        ushort length = (ushort)offset;
        packet[2] = (byte)(length >> 8); // Horní bajt délky
        packet[3] = (byte)length;        // Dolní bajt délky

        // Oříznutí pole na skutečnou velikost
        Array.Resize(ref packet, length);
        return packet;
    }

    static byte[] EncryptRadiusPassword(byte[] password, byte[] requestAuthenticator, string secret)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        // Výpočet velikosti zašifrovaného hesla (zaokrouhleno na násobek 16)
        byte[] encrypted = new byte[16 * ((password.Length + 15) / 16)];

        // Iterace přes 16-bajtové bloky
        for (int i = 0; i < encrypted.Length; i += 16)
        {
            byte[] block = new byte[16];
            int copyLength = Math.Min(16, password.Length - i);
            if (copyLength > 0)
                Array.Copy(password, i, block, 0, copyLength);

            // Vytvoření MD5 hashe pro XOR šifrování
            using (MD5 md5 = MD5.Create())
            {
                // První část hashe: sdílený secret
                md5.TransformBlock(secretBytes, 0, secretBytes.Length, null, 0);
                // Druhá část hashe: request authenticator (první blok) nebo předchozí ciphertext
                if (i == 0)
                    md5.TransformFinalBlock(requestAuthenticator, 0, requestAuthenticator.Length);
                else
                    md5.TransformFinalBlock(encrypted, i - 16, 16);

                byte[] hash = md5.Hash;
                // XOR mezi plaintext blokem a MD5 hashem
                for (int j = 0; j < 16; j++)
                    encrypted[i + j] = (byte)(block[j] ^ hash[j]);
            }
        }

        return encrypted;
    }

    static void ParseRadiusResponse(byte[] data, string secret)
    {
        if (data.Length < 20) return; // Základní kontrola velikosti odpovědi

        // Interpretace kódu odpovědi
        byte code = data[0];
        string codeName = code switch
        {
            2 => "Access-Accept",
            3 => "Access-Reject",
            11 => "Access-Challenge",
            _ => "Unknown"
        };

        Console.WriteLine($"RADIUS Response: {codeName} (Code: {code})");

        // Výsledek autentizace
        if (code == 2)
            Console.WriteLine("✅ Autentizace ÚSPĚŠNÁ");
        else if (code == 3)
            Console.WriteLine("❌ Autentizace ZAMÍTNUTA");
    }
}

/*
Struktura RADIUS paketu:
 První bajt: Kód typu zprávy (1 = Access-Request)
 Druhý bajt: Identifikátor pro spárování požadavku s odpovědí
 Třetí a čtvrtý bajt: Celková délka paketu
 16 bajtů: Request Authenticator (náhodná data)
Šifrování hesla:
 Používá algoritmus popsaný v RFC 2865
 Heslo je rozděleno na 16-bajtové bloky
 Každý blok je XORován s MD5 hashem kombinujícím:
 Sdílený secret
 Request Authenticator (pro první blok)
 Předchozí ciphertext (pro další bloky)
Atributy:
 Každý atribut má formát [Type][Length][Value]
 User-Name (1): Uživatelské jméno v plaintextu
 User-Password (2): Zašifrované heslo
 NAS-Identifier (32): Identifikace RADIUS klienta
Zpracování odpovědi:
 Analyzuje se první bajt pro typ odpovědi
 Access-Accept (2): Úspěšná autentizace
 Access-Reject (3): Neúspěšná autentizace

Tento kód implementuje základní RADIUS klient podle RFC 2865, ale pro produkční použití by bylo vhodné přidat:
 Ověření Response Authenticator
 Podporu více atributů
 Lepší manipulaci s chybami
 Zabezpečení proti replay útokům
*/