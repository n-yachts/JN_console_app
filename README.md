# Síťové diagnostické nástroje a utility

Kolekce síťových nástrojů pro diagnostiku, monitoring a správu sítí.

## **Síťové diagnostické nástroje**

### **AdvancedTelnetClient**
Pokročilý Telnet klient s podporou více protokolů, barevného výstupu a skriptování. Umožňuje připojení k různým síťovým službám (SSH, Telnet, RAW TCP) s pokročilými funkcemi jako logování, automatizace příkazů a podpora různých kódování.

### **ArPing / ArpPing**
Odesílá ARP (Address Resolution Protocol) požadavky pro zjištění dostupnosti zařízení v lokální síti. Na rozdíl od ICMP pingu funguje i když je ICMP blokované, protože pracuje na linkové vrstvě.

### **ARPTable**
Zobrazuje obsah ARP cache systému - tabulku mapování IP adres na MAC adresy v lokální síti. Užitečné pro diagnostiku síťových konfliktů a analýzu topologie sítě.

### **BandwidthMonitor / BandwidthMonitor_2**
Monitoruje využití síťové šířky pásma v reálném čase. Zobrazuje přenosové rychlosti na jednotlivých síťových rozhraních, celkový přenos a historické statistiky.

### **CustomPing / MiniPing**
Vlastní implementace ICMP ping nástroje. Odesílá ICMP Echo Request pakety a měří dobu odpovědi, ztrátovost paketů a TTL (Time to Live).

### **CustomTraceroute / TraceRoute**
Sleduje cestu paketů od zdroje k cíli přes jednotlivé směrovače. Identifikuje síťové úzkosti a problémy se směrováním.

### **IPCalculator / SubnetCalculator**
Vypočítává síťové parametry z IP adresy a masky - síťovou adresu, broadcast, rozsah použitelných IP, počet hostů a další subnetting informace.

### **LatencyMonitor**
Průběžně monitoruje latenci (odezvu) k více síťovým cílům současně. Detekuje výpadky a kolísání odezvy v čase.

### **PortScanner**
Skenuje rozsah portů na cílovém zařízení a identifikuje otevřené porty a běžící služby. Užitečné pro bezpečnostní audity a inventarizaci služeb.

### **TCPConnectionMonitor**
Zobrazuje aktivní TCP spojení na lokálním počítači - lokální/vzdálené adresy, porty, stav spojení a procesy.

### **TopologyMapper**
Automaticky mapuje síťovou topologii skenováním IP rozsahů a identifikací aktivních zařízení. Vytváří přehled o struktuře sítě.

## **Protokolové klienty a servery**

### **ChatClient / ChatServer**
Jednoduchý chatovací systém pomocí TCP socketů. Server přijímá připojení více klientů a přeposílá zprávy mezi nimi.

### **DHCP Client Simulator**
Simuluje DHCP klienta - odesílá DHCP Discover, Request a Renew zprávy pro testování DHCP serverů a analýzu síťové konfigurace.

### **DNSBlackhole**
Jednoduchý DNS server, který blokuje přístup na škodlivé nebo nežádoucí domény vracením falešných odpovědí.

### **DNSResolver / SimpleDNSServer**
Překládá doménová jména na IP adresy a naopak. SimpleDNSServer je základní implementace DNS serveru.

### **HTTPChecker / HeaderAnalyzer**
HTTPChecker testuje dostupnost webových služeb, HeaderAnalyzer detailně analyzuje HTTP hlavičky odpovědí serverů.

### **LdapBrowser**
Prohlížeč LDAP (Lightweight Directory Access Protocol) adresářů - umožňuje procházení a dotazování na directory služby jako Active Directory.

### **ModbusScanner**
Skenuje a testuje zařízení používající Modbus průmyslový protokol. Detekuje podporované funkce a čte registry.

### **MulticastListener**
Připojuje se k multicast skupinám a přijímá multicast datagramy. Užitečné pro testování multicast aplikací a síťového vysílání.

### **RadiusClient**
Klient pro RADIUS (Remote Authentication Dial-In User Service) protokol - testuje autentizaci proti RADIUS serverům.

### **ServiceFingerprinter**
Identifikuje síťové služby na základě jejich bannerů a odpovědí. Rozpoznává typ a verzi služby běžící na daném portu.

### **SimpleFTPClient**
Základní FTP klient pro přenos souborů - podporuje základní FTP operace jako upload, download a listování adresářů.

### **SimpleHTTPServer**
Jednoduchý HTTP server schopný obsluhovat webové požadavky a servírovat statický obsah.

### **SimpleMQTTClient**
Klient pro MQTT (Message Queuing Telemetry Transport) protokol používaný v IoT - připojuje se k brokerům a publikuje/odebírá zprávy.

### **SimpleSniffer**
Základní síťový sniffer, který zachytává a analyzuje síťový provoz na zvoleném rozhraní.

### **SipAnalyzer**
Analyzuje SIP (Session Initiation Protocol) provoz používaný pro VoIP komunikaci - zachytává a dekóduje SIP zprávy.

### **SnmpWalker**
Prochází SNMP (Simple Network Management Protocol) MIB stromy zařízení - čte a zobrazuje hodnoty z říditelných objektů.

### **TelnetClient / TelnetServer**
Kompletní Telnet klient a server s podporou základních Telnet příkazů a řídicích sekvencí.

### **WhoisClient**
Dotazuje se WHOIS databází na informace o doménových jménech a IP adresách - vlastníci, kontakty, datum registrace.

## **Monitorovací a bezpečnostní nástroje**

### **BlockchainMonitor**
Monitoruje stav blockchainových uzlů (Bitcoin, Ethereum) - kontroluje synchronizaci, počet připojení a stav sítě.

### **CertificateExpiryChecker**
Kontroluje platnost SSL/TLS certifikátů na vzdálených serverech a varuje před blížící se expirací.

### **ContainerNetworkInspector**
Analyzuje síťovou konfiguraci Docker kontejnerů - sítě, IP adresy, propojení a síťové bridge.

### **InterfaceMonitor**
Průběžně monitoruje stav a statistiku síťových rozhraní - přenosové rychlosti, chyby, stav spojení.

### **MACResolver**
Překládá MAC adresy na výrobce zařízení pomocí OUI (Organizationally Unique Identifier) databáze.

### **NetworkDocumenter / NetworkInfo**
Generuje kompletní dokumentaci o síťové konfiguraci systému - rozhraní, IP adresy, routing, DNS servery, síťové statistiky.

### **ProxyDetector**
Detekuje použití proxy serverů a analyzuje jejich konfiguraci. Identifikuje transparentní proxy a NAT.

### **SSLChecker**
Detailně analyzuje SSL/TLS certifikáty - vydavatele, platnost, podpisové algoritmy a slabiny konfigurace.

### **ThroughputTester / TrafficGenerator**
Měří síťovou propustnost odesíláním testovacích dat a měřením přenosové rychlosti. TrafficGenerator vytváří zátěžový provoz.

### **WakeOnLAN**
Odesílá Wake-on-LAN "magic packet" pro vzdálené probuzení zařízení z režimu spánku.

### **WifiScanner**
Skenuje dostupné WiFi sítě a zobrazuje jejich parametry - SSID, sílu signálu, kanál, šifrování a BSSID.

## **Speciální nástroje**

### **NtpClient**
Synchronizuje čas s NTP (Network Time Protocol) servery a měří přesnost časové synchronizace.

### **SimpleFileServer**
Jednoduchý souborový server pro sdílení souborů přes síť s základní autentizací a přístupovými právy.

### **SimpleWebCrawler**
Základní webový crawler, který prochází webové stránky a extrahuje odkazy - užitečné pro mapování webových aplikací.
