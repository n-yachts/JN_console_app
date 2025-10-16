using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class RadiusClient
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Použití: RadiusClient <server> <secret> <username> <password>");
            Console.WriteLine("Příklad: RadiusClient 192.168.1.1 secret123 user1 pass123");
            return;
        }

        string server = args[0];
        string secret = args[1];
        string username = args[2];
        string password = args[3];

        try
        {
            using (UdpClient client = new UdpClient())
            {
                IPEndPoint radiusEndpoint = new IPEndPoint(IPAddress.Parse(server), 1812);

                // Vytvoření RADIUS Access-Request packetu
                byte[] requestPacket = CreateRadiusRequest(secret, username, password);

                Console.WriteLine($"Odesílám RADIUS request pro uživatele '{username}'");
                client.Send(requestPacket, requestPacket.Length, radiusEndpoint);

                // Čekání na odpověď
                client.Client.ReceiveTimeout = 5000;
                IPEndPoint responseEndpoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] responseData = client.Receive(ref responseEndpoint);

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
        byte[] packet = new byte[4096];
        byte[] requestAuthenticator = new byte[16];
        random.NextBytes(requestAuthenticator);

        // RADIUS header
        packet[0] = 1; // Code: Access-Request
        packet[1] = (byte)random.Next(256); // Identifier
        packet[2] = 0; // Length (will be set later)
        packet[3] = 0;

        // Request Authenticator
        Array.Copy(requestAuthenticator, 0, packet, 4, 16);

        int offset = 20;

        // User-Name attribute (1)
        byte[] usernameBytes = Encoding.UTF8.GetBytes(username);
        packet[offset++] = 1; // Type
        packet[offset++] = (byte)(usernameBytes.Length + 2); // Length
        Array.Copy(usernameBytes, 0, packet, offset, usernameBytes.Length);
        offset += usernameBytes.Length;

        // User-Password attribute (2) - encrypted with MD5
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] encryptedPassword = EncryptRadiusPassword(passwordBytes, requestAuthenticator, secret);
        packet[offset++] = 2; // Type
        packet[offset++] = (byte)(encryptedPassword.Length + 2); // Length
        Array.Copy(encryptedPassword, 0, packet, offset, encryptedPassword.Length);
        offset += encryptedPassword.Length;

        // NAS-Identifier attribute (32)
        byte[] nasId = Encoding.UTF8.GetBytes("radius-client");
        packet[offset++] = 32; // Type
        packet[offset++] = (byte)(nasId.Length + 2); // Length
        Array.Copy(nasId, 0, packet, offset, nasId.Length);
        offset += nasId.Length;

        // Set length
        ushort length = (ushort)offset;
        packet[2] = (byte)(length >> 8);
        packet[3] = (byte)length;

        Array.Resize(ref packet, length);
        return packet;
    }

    static byte[] EncryptRadiusPassword(byte[] password, byte[] requestAuthenticator, string secret)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        byte[] encrypted = new byte[16 * ((password.Length + 15) / 16)];

        for (int i = 0; i < encrypted.Length; i += 16)
        {
            byte[] block = new byte[16];
            int copyLength = Math.Min(16, password.Length - i);
            if (copyLength > 0)
                Array.Copy(password, i, block, 0, copyLength);

            using (MD5 md5 = MD5.Create())
            {
                md5.TransformBlock(secretBytes, 0, secretBytes.Length, null, 0);
                if (i == 0)
                    md5.TransformFinalBlock(requestAuthenticator, 0, requestAuthenticator.Length);
                else
                    md5.TransformFinalBlock(encrypted, i - 16, 16);

                byte[] hash = md5.Hash;
                for (int j = 0; j < 16; j++)
                    encrypted[i + j] = (byte)(block[j] ^ hash[j]);
            }
        }

        return encrypted;
    }

    static void ParseRadiusResponse(byte[] data, string secret)
    {
        if (data.Length < 20) return;

        byte code = data[0];
        string codeName = code switch
        {
            2 => "Access-Accept",
            3 => "Access-Reject",
            11 => "Access-Challenge",
            _ => "Unknown"
        };

        Console.WriteLine($"RADIUS Response: {codeName} (Code: {code})");

        if (code == 2)
            Console.WriteLine("✅ Autentizace ÚSPĚŠNÁ");
        else if (code == 3)
            Console.WriteLine("❌ Autentizace ZAMÍTNUTA");
    }
}