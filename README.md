# Síťové diagnostické nástroje a utility

Kolekce malých konzolových aplikací (C#, .NET 8 a .NET 10, jen Windows) pro diagnostiku, monitoring a testování sítí. Každý nástroj je samostatný projekt v řešení `JN_console_app.slnx` a spouští se z příkazové řádky.

**Konvence zápisu parametrů:** `<povinný>`, `[volitelný]`. Programy bez parametrů se spouští jen názvem. Programy označené ⚠️ vyžadují spuštění jako správce.

**Externí závislosti:** `CDP_LLDP_Scanner` potřebuje nainstalovaný Npcap (balíček SharpPcap). `LdapBrowser` používá `System.DirectoryServices.Protocols`. `Nmea0183Reader` a `Nmea2000Reader` používají NuGet balíček `System.IO.Ports`. Ostatní projekty používají jen základní knihovny .NET.

---

## Diagnostika sítě (ICMP, ARP, směrování)

| Program | Co dělá | Parametry |
|---|---|---|
| **AdvancedPing** | ICMP ping: posílá Echo Request (32 B, bez fragmentace), vypisuje dobu odezvy, TTL a statistiku ztrát. | `<adresa> [timeout_ms=1000] [počet=4]`<br>př. `AdvancedPing 192.168.1.1 500 10` |
| **ArpPing** | Zjistí MAC adresu zařízení v lokální síti přes ARP (`SendARP`); funguje i při blokovaném ICMP. Jen IPv4, ne multicast/broadcast. | `<IPv4 adresa>` |
| **ARPTable** | Vypíše ARP cache systému (IP, MAC, typ dynamický/statický, rozhraní) přes Win32 API. | – |
| **TraceRoute** | Trasování cesty paketů ke cíli pomocí rostoucího TTL (max. 30 skoků, timeout 1 s). | `<hostname/IP>` |
| **CustomTraceroute** | Další implementace traceroute (max. 30 skoků, timeout 1 s) přes `Ping` s nastaveným TTL a zakázanou fragmentací. | `<cíl>` |
| **LatencyMonitor** | Průběžně pinguje více cílů najednou (timeout 1 s), ukončení Ctrl+C. | `<host1> [host2 ...]` |
| **TopologyMapper** | Pingem prochází všechny hosty v podsíti (po dávkách po 50) a vypíše aktivní zařízení. Prefix /1–/30. | `<síť/CIDR>`<br>př. `TopologyMapper 192.168.1.0/24` |
| **PortScanner** | TCP scan nejběžnějších portů (21, 22, 23, 25, 53, 80, 110, 143, 443, 993, 995) – otevřený / zavřený / filtrovaný. | `<hostname/IP>` |
| **ServiceFingerprinter** | Identifikuje službu na portu: u HTTP (80, 443, 8080) pošle `GET /` a vypíše odpověď (u 443 přes TLS), u ostatních čeká 3 s na banner. | `<host> <port>` |
| **HostName** | Reverzní DNS – zjistí jméno hostitele z IPv4 adresy. | `<IPv4 adresa>` |
| **NetBIOSNameResolver** | Zjistí NetBIOS jméno z IPv4 adresy – nejprve `nbtstat`, poté přímý dotaz NBSTAT na UDP 137. Cíl musí mít zapnutý NetBIOS over TCP/IP. | `<IPv4 adresa>` |
| **MACResolver** | Zjistí MAC adresu k IP/hostname – umí **jen adresy lokálních rozhraní** (ne vzdálených zařízení). | `<IP/hostname>` |
| **DNSResolver** | Přeloží jméno na IP adresy (A/AAAA) přes systémový resolver. | `<hostname>` |
| **WhoisClient** | WHOIS dotaz (TCP 43) na `whois.iana.org`; vrací údaje o TLD a řádek `refer:` s koncovým registrem (dotaz na něj se neprovádí). | `<doména>` |

## Informace o systému a rozhraních

| Program | Co dělá | Parametry |
|---|---|---|
| **NetworkInfo** | Hostname a seznam rozhraní s typem, popisem, IPv4 adresou a maskou. | – |
| **NetworkDocumenter** | Textový report: rozhraní (MAC, rychlost, IPv4/maska), výchozí brány, DNS servery a počty aktivních TCP/UDP spojení a listenerů. | – |
| **InterfaceMonitor** | Každé 2 s vypisuje stav, rychlost a přijatá/odeslaná data všech rozhraní (Ctrl+C ukončí). | – |
| **BandwidthMonitor** | Měří propustnost každého aktivního (ne-loopback) rozhraní v reálném čase (každou 1 s), Ctrl+C ukončí. | – |
| **TCPConnectionMonitor** | Vypíše aktivní TCP spojení (lokální/vzdálený koncový bod a stav). Nezobrazuje procesy. | – |
| **WifiScanner** | Skenuje WiFi sítě přes `netsh wlan show networks mode=bssid` – SSID, signál, autentizace, kanál, BSSID. Může vyžadovat správce a zapnuté služby určování polohy. | – |
| **CDP_LLDP_Scanner** | Pasivně zachytává CDP a LLDP pakety (SharpPcap + Npcap) a zobrazí informace o sousedním přepínači (hostname, port, platforma, capabilities, chassis ID…). Adaptér se vybírá interaktivně, ENTER ukončí. | – (interaktivní výběr adaptéru) |
| **SimpleSniffer** ⚠️ | Zachytává IP pakety (ICMP/TCP/UDP…) na raw socketu v promiskuitním režimu a vypisuje zdroj, cíl, protokol a velikost. Jen Windows, jen IPv4. | – |

## Výpočty a konfigurace

| Program | Co dělá | Parametry |
|---|---|---|
| **IPCalculator** | Z IP a masky spočítá síťovou adresu, broadcast, rozsah použitelných adres a počet hostů. | `<IP/maska>` (maska 0–32)<br>př. `IPCalculator 192.168.1.0/24` |
| **SubnetCalculator** | Spočítá novou masku (prefix) podsítě a skutečnou kapacitu pro požadovaný počet hostů. | `<IP/maska> <počet hostů>`<br>př. `SubnetCalculator 192.168.1.0/24 50` |
| **WakeOnLAN** | Odešle Wake-on-LAN magic packet (UDP broadcast). MAC lze zadat s `:` i `-`. | `<MAC adresa>` |
| **NtpClient** | Porovná lokální čas se servery pool.ntp.org, time.google.com, time.windows.com, time.nist.gov, tik/tak.cesnet.cz a vypíše rozdíl v ms. Čas **nenastavuje**. | – |

## Web, TLS a certifikáty

| Program | Co dělá | Parametry |
|---|---|---|
| **HTTPChecker** | GET požadavek; vypíše stavový kód a hlavičku `Server`. | `<URL>` |
| **HeaderAnalyzer** | GET požadavek a výpis všech hlaviček odpovědi (hlavičky odpovědi i obsahu). Doplní `http://`, pokud chybí schéma. | `<URL>` |
| **SSLChecker** | Zobrazí údaje TLS certifikátu: subjekt, vydavatel, platnost od/do a SHA1 otisk. Výchozí port 443. | `<hostname[:port]>` |
| **CertificateExpiryChecker** | Zjistí platnost certifikátu a zbývající dny; varuje při expiraci < 30 dní nebo prošlém certifikátu. Výchozí port 443. | `<hostname[:port]>` |
| **ProxyDetector** | Dotáže se služeb ip-api.com a ipinfo.io na IP/hostname a vypíše surové JSON (příznaky proxy/hosting; detekce VPN/proxy u ipinfo jen v placených tarifech). | `<IP/hostname>` |
| **SimpleWebCrawler** | Rekurzivně prochází odkazy v rámci domény startovní stránky (max. hloubka 3, max. 50 stránek). | `<start_url>`<br>př. `SimpleWebCrawler https://example.com` |

## Klienti protokolů

| Program | Co dělá | Parametry |
|---|---|---|
| **TelnetClient** | Telnet klient se zpracováním IAC sekvencí. Ukončení: `QUIT`, Esc nebo Ctrl+C. | `<hostname> <port>` |
| **AdvancedTelnetClient** | Telnet klient s negociací voleb (ECHO, SGA), barevným výstupem a příkazy `QUIT` a `CLEAR`; ukončení Esc/Ctrl+C. | `<hostname> <port>`<br>př. `AdvancedTelnetClient localhost 23` |
| **SimpleFTPClient** | Přihlásí se na FTP server a vypíše obsah kořenového adresáře (pouze `LIST`, bez uploadu/downloadu). | `<server> <username> <password>` |
| **LdapBrowser** | Připojí se k LDAP (Basic bind, protokol v3; port 636 = LDAPS), vyhledá všechny objekty v podstromu (stránkování po 500). Na jiném portu než 636 posílá heslo nešifrovaně. | `<server> <port> <username> <password> [searchBase]` |
| **RadiusClient** | Odešle RADIUS Access-Request (UDP 1812) a ověří Response Authenticator; vypíše úspěch/zamítnutí. Jméno max. 253 B, heslo max. 128 B. | `<server> <secret> <username> <password>` |
| **SnmpWalker** | SNMP GET-NEXT walk od zadaného OID (UDP 161). | `<host> <community> <startOID> <timeout_ms>`<br>př. `SnmpWalker 192.168.1.1 public 1.3.6.1.2.1.1 5000` |
| **DhcpClientSimulator** | Odešle DHCP Discover (broadcast, port 67, naslouchá na 68; timeout 5 s) a vypíše typ odpovědi, nabízenou IP a server. | – |
| **ModbusScanner** | Připojí se na Modbus TCP (port 502) a otestuje funkce 1–4 (čtení); vypíše, které zařízení podporuje (Illegal Function = nepodporováno), a dekóduje holding registry. | `<host>`<br>př. `ModbusScanner 192.168.1.100` |
| **SipAnalyzer** | Naslouchá na UDP portu a dekóduje příchozí SIP zprávy (požadavky/odpovědi, hlavičky). | `<lokální_port>`<br>př. `SipAnalyzer 5060` |
| **MulticastListener** | Připojí se k multicast skupině na všech vhodných rozhraních a vypisuje příchozí datagramy. | `<multicast_skupina> <port>`<br>př. `MulticastListener 224.0.0.1 5000` |

## Servery

| Program | Co dělá | Parametry |
|---|---|---|
| **ChatServer** | TCP chat server – přijímá klienty a přeposílá zprávy. Pevný port **8080**. | – |
| **ChatClient** | Klient k ChatServeru – posílá a přijímá zprávy. | `<server> <port>` |
| **TelnetServer** | Telnet server napodobující CLI síťového zařízení (hostname `Router`): `show running-config`, `show version`, `show interfaces`, `show arp`, `enable`, `disable`, `configure terminal`, `hostname <jméno>`, `exit`, `help`/`?`. Ukončení Ctrl+C. | `[port=23]` |
| **SimpleHTTPServer** | HTTP server na portu **8080**; na každý požadavek vrací HTML stránku s URL a metodou. | – |
| **SimpleFileServer** | TCP server na portu **8080** sdílející adresář `./shared` (vytvoří se automaticky), vypisuje seznam souborů. Bez autentizace. | – |
| **DNSBlackhole** ⚠️ | DNS server na UDP 53; domény `malware.com`, `ads.example.com`, `tracker.com` (i subdomény) vrací jako `0.0.0.0`, ostatní překládá systémovým DNS. Seznam je v kódu. | – |
| **SimpleDNSServer** | Pouze demonstrace – přeloží několik pevných domén (google.com, seznam.cz, github.com); skutečný DNS server to **není**. | – |

## Měření a generování provozu

| Program | Co dělá | Parametry |
|---|---|---|
| **ThroughputTester** | Odešle po TCP náhodná data a změří propustnost v Mbps. Na druhé straně musí běžet server, který data přijme (např. discard). | `<server> <port> <velikost_MB>` (1–2047) |
| **TrafficGenerator** | Odesílá UDP pakety s textem `Test packet` (jeden každých 100 ms) na cíl. | `<cíl> <port> <počet paketů>` |

## Námořní protokoly (sériová linka)

| Program | Co dělá | Vstup |
|---|---|---|
| **Nmea0183Reader** | Čte NMEA 0183 věty ze sériového portu a dekóduje GGA, RMC, GSA, GSV, VTG. Konec klávesou `q`. | Interaktivně: název portu (např. `COM3`), baud rate (výchozí 4800; 8N1) |
| **Nmea2000Reader** | Čte zjednodušený rámec NMEA 2000 `[PGN 3 B little-endian][zdroj 1 B][data max. 8 B]` a dekóduje PGN 129025, 129026, 129539, 129033, 127250, 128259, 128267, 130306. Fast-packet PGN (např. 129029) nepodporuje. Konec klávesou `q`. | Interaktivně: port (číslo ze seznamu nebo název), baud rate (výchozí 115200) |

---

## Sestavení

Všechny projekty cílí na `net8.0` i `net10.0` (společné nastavení je v `Directory.Build.props`). Je potřeba .NET SDK 10 (umí sestavit oba cíle).

Spustitelné soubory (jeden `.exe` na program, bez doprovodných `.dll`) vzniknou publikací pro každý cíl zvlášť:

```
dotnet publish JN_console_app.slnx -c Release -f net8.0
dotnet publish JN_console_app.slnx -c Release -f net10.0
```

Výstup je v `Release\net8.0\` a `Release\net10.0\` (všechny programy v jedné složce, např. `Release\net10.0\PortScanner.exe example.com`). Soubory jsou závislé na nainstalovaném běhovém prostředí (framework-dependent, win-x64), takže na cílovém počítači musí být .NET 8 resp. .NET 10 Runtime.
