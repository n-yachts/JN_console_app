using PacketDotNet;  // Knihovna pro parsování síťových paketů (EthernetPacket, Packet)
using SharpPcap;  // Knihovna pro zachytávání paketů (obal nad Npcap/libpcap)
using System;  // Základní jmenný prostor pro Console, Array, BitConverter
using System.Text;  // Práce s kódováním textu (Encoding.ASCII)

namespace CDP_LLDP_Scanner  // Jmenný prostor projektu
{
    class CDP_LLDP_Scanner  // Hlavní třída programu - pasivní skener sousedních zařízení
    {
        static void Main(string[] args)  // Hlavní vstupní bod programu
        {
            // Získání seznamu všech zachytávacích zařízení (síťových adaptérů) dostupných v systému
            var devices = CaptureDeviceList.Instance;

            // Kontrola, zda je k dispozici alespoň jeden adaptér (vyžaduje nainstalovaný Npcap)
            if (devices.Count < 1)
            {
                Console.WriteLine("Nebyla nalezena žádná síťová zařízení.");
                return;  // Ukončení programu
            }

            Console.WriteLine("Dostupné adaptéry:\n");

            // Výpis všech adaptérů s jejich pořadovým číslem
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"{i}) {devices[i].Description}");
            }

            // Výběr adaptéru uživatelem (neplatný vstup se ošetří níže)
            Console.Write("\nVyber adaptér: ");
            if (!int.TryParse(Console.ReadLine(), out int index) || index < 0 || index >= devices.Count)
            {
                Console.WriteLine("Neplatná volba adaptéru.");
                return;  // Ukončení programu při neplatné volbě
            }

            var device = devices[index];  // Vybraný adaptér

            // Registrace obsluhy události, která se zavolá pro každý zachycený paket
            device.OnPacketArrival += Device_OnPacketArrival;

            int readTimeoutMilliseconds = 1000;  // Časový limit čtení z adaptéru v milisekundách

            // Otevření adaptéru v promiskuitním režimu (zachytává i pakety určené jiným stanicím)
            device.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);

            // Filtr paketů (syntaxe BPF):
            // LLDP má EtherType 0x88cc
            // CDP je rámec 802.3 + LLC/SNAP (PID 0x2000) odeslaný na multicast 01:00:0c:cc:cc:cc
            device.Filter = "ether proto 0x88cc or ether dst 01:00:0c:cc:cc:cc";

            Console.WriteLine("\nPoslouchám CDP/LLDP pakety...");
            Console.WriteLine("Stiskni ENTER pro ukončení.\n");

            device.StartCapture();  // Zahájení zachytávání na pozadí

            Console.ReadLine();  // Čekání na stisk Enter

            device.StopCapture();  // Zastavení zachytávání
            device.Close();  // Uvolnění adaptéru
        }

        // Obsluha události - zavolá se pro každý zachycený paket
        private static void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            try  // Chyba v jednom paketu nesmí ukončit celý program
            {
                var rawPacket = e.GetPacket();  // Surová data paketu
                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);  // Rozparsování podle typu linkové vrstvy

                var ethernetPacket = packet.Extract<EthernetPacket>();  // Získání Ethernet hlavičky

                if (ethernetPacket == null)  // Paket není Ethernet
                    return;

                ushort etherType = (ushort)ethernetPacket.Type;  // EtherType (u rámců 802.3 je to délka rámce)

                // LLDP (IEEE 802.1AB) - EtherType 0x88cc
                if (etherType == 0x88cc)
                {
                    ParseLLDP(ethernetPacket.PayloadData);
                }

                // CDP (Cisco) - za Ethernet hlavičkou následuje LLC/SNAP hlavička o 8 bajtech:
                // AA AA 03 (LLC), 00 00 0C (OUI Cisco), 20 00 (PID CDP); poté následují TLV záznamy
                byte[] payload = ethernetPacket.PayloadData;
                if (etherType != 0x88cc && payload != null && payload.Length > 8 &&
                    payload[0] == 0xAA && payload[1] == 0xAA && payload[2] == 0x03 &&
                    payload[6] == 0x20 && payload[7] == 0x00)
                {
                    var cdpData = new byte[payload.Length - 8];  // Data CDP bez LLC/SNAP hlavičky
                    Array.Copy(payload, 8, cdpData, 0, cdpData.Length);
                    ParseCDP(cdpData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba: {ex.Message}");  // Výpis chyby zpracování paketu
            }
        }

        // Zpracování LLDP - data jsou posloupnost TLV (Type-Length-Value) záznamů
        static void ParseLLDP(byte[] data)
        {
            Console.WriteLine("========== LLDP ==========");

            int offset = 0;  // Aktuální pozice v datech

            // Cyklus přes všechny TLV záznamy (hlavička TLV má 2 bajty)
            while (offset + 2 <= data.Length)
            {
                // TLV hlavička: 7 bitů typ + 9 bitů délka hodnoty
                ushort tlvHeader = (ushort)((data[offset] << 8) | data[offset + 1]);

                int type = (tlvHeader >> 9) & 0x7F;  // Horních 7 bitů = typ
                int length = tlvHeader & 0x1FF;  // Dolních 9 bitů = délka hodnoty

                offset += 2;  // Přeskočení hlavičky TLV

                if (offset + length > data.Length)  // Poškozený záznam - hodnota přesahuje data
                    break;

                byte[] value = new byte[length];  // Hodnota TLV
                Array.Copy(data, offset, value, 0, length);

                switch (type)
                {
                    case 1:  // Chassis ID
                        Console.WriteLine($"Chassis ID: {FormatLldpId(value)}");
                        break;

                    case 2:  // Port ID
                        Console.WriteLine($"Port ID: {FormatLldpId(value)}");
                        break;

                    case 5:  // System Name (jméno zařízení)
                        Console.WriteLine($"Hostname: {Encoding.ASCII.GetString(value)}");
                        break;

                    case 6:  // System Description
                        Console.WriteLine($"Description: {Encoding.ASCII.GetString(value)}");
                        break;

                    case 8:  // Management Address (vypíše se surově v hex)
                        Console.WriteLine($"Management Address: {BitConverter.ToString(value)}");
                        break;
                }

                offset += length;  // Přechod na další TLV

                if (type == 0)  // Typ 0 = End of LLDPDU (konec dat)
                    break;
            }

            Console.WriteLine();
        }

        // Chassis ID / Port ID: první bajt je subtype, zbytek je vlastní identifikátor
        static string FormatLldpId(byte[] value)
        {
            if (value.Length < 2)  // Příliš krátká hodnota - vypíše se hex
                return BitConverter.ToString(value);

            byte subtype = value[0];  // Určuje význam zbytku (MAC adresa, jméno rozhraní, ...)
            var id = new byte[value.Length - 1];  // Vlastní identifikátor bez subtype
            Array.Copy(value, 1, id, 0, id.Length);

            // MAC adresa (Chassis subtype 4, Port subtype 3) se zobrazí ve tvaru AA:BB:CC:DD:EE:FF
            if ((subtype == 3 || subtype == 4) && id.Length == 6)
                return BitConverter.ToString(id).Replace('-', ':');

            return Encoding.ASCII.GetString(id);  // Ostatní subtypy jsou textové (např. jméno rozhraní)
        }

        // Zpracování CDP - po 4bajtové hlavičce (verze, TTL, kontrolní součet) následují TLV záznamy
        static void ParseCDP(byte[] data)
        {
            Console.WriteLine("========== CDP ==========");

            if (data.Length < 4)  // Chybí hlavička
                return;

            int offset = 4;  // Přeskočení hlavičky CDP

            // Cyklus přes všechny TLV záznamy (hlavička TLV má 4 bajty: typ 2 B + délka 2 B)
            while (offset + 4 <= data.Length)
            {
                ushort type = (ushort)((data[offset] << 8) | data[offset + 1]);  // Typ TLV
                ushort length = (ushort)((data[offset + 2] << 8) | data[offset + 3]);  // Délka TLV včetně 4bajtové hlavičky

                if (length <= 4 || offset + length > data.Length)  // Poškozený záznam
                    break;

                int valueLength = length - 4;  // Délka vlastní hodnoty

                byte[] value = new byte[valueLength];
                Array.Copy(data, offset + 4, value, 0, valueLength);

                switch (type)
                {
                    case 0x0001:  // Device ID (jméno zařízení)
                        Console.WriteLine($"Hostname: {Encoding.ASCII.GetString(value)}");
                        break;

                    case 0x0003:  // Port ID (odesílající rozhraní)
                        Console.WriteLine($"Port: {Encoding.ASCII.GetString(value)}");
                        break;

                    case 0x0004:  // Capabilities (bitová maska schopností - router, switch, ...)
                        Console.WriteLine($"Capabilities: {BitConverter.ToString(value)}");
                        break;

                    case 0x0006:  // Platform (model zařízení)
                        Console.WriteLine($"Platform: {Encoding.ASCII.GetString(value)}");
                        break;
                }

                offset += length;  // Přechod na další TLV
            }

            Console.WriteLine();
        }
    }
}

/*
Princip: pasivní zachytávání (SharpPcap + Npcap) a zobrazení informací o sousedních zařízeních
LLDP (IEEE 802.1AB):
 EtherType 0x88cc, data jsou TLV záznamy (7 bitů typ, 9 bitů délka)
 Chassis ID a Port ID začínají bajtem subtype (např. 4 = MAC adresa)
CDP (Cisco Discovery Protocol):
 Rámec 802.3 s LLC/SNAP hlavičkou (OUI 00000C, PID 0x2000), multicast 01:00:0c:cc:cc:cc
 Hlavička 4 B, poté TLV záznamy (typ 2 B, délka 2 B včetně hlavičky)
Poznámky:
 Zařízení vysílají CDP/LLDP obvykle jednou za 30–60 s, je potřeba chvíli počkat
 Zachytávání vyžaduje nainstalovaný Npcap a spuštění s odpovídajícími právy
*/
