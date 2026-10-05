# Síťové diagnostické nástroje a utility

Kolekce malých konzolových aplikací (C#, .NET 8 a .NET 10, Windows a Linux) pro diagnostiku, monitoring a testování sítí. Každý nástroj je samostatný projekt v řešení `JN_console_app.slnx` a spouští se z příkazové řádky.

**Konvence zápisu parametrů:** `<povinný>`, `[volitelný]`. Programy bez parametrů se spouští jen názvem. Programy označené ⚠️ vyžadují spuštění jako správce.

**Externí závislosti:** `CDP_LLDP_Scanner` potřebuje nainstalovaný Npcap (Windows) nebo libpcap (Linux), viz kapitola Linux. `LdapBrowser` používá `System.DirectoryServices.Protocols`. `Nmea0183Reader` a `Nmea2000Reader` používají NuGet balíček `System.IO.Ports`. Ostatní projekty používají jen základní knihovny .NET.

---

## Diagnostika sítě (ICMP, ARP, směrování)

| Program | Co dělá | Parametry |
|---|---|---|
| **AdvancedPing** | ICMP ping: posílá Echo Request (32 B, bez fragmentace), vypisuje dobu odezvy, TTL a statistiku ztrát. | `<adresa> [timeout_ms=1000] [počet=4]`<br>př. `AdvancedPing 192.168.1.1 500 10` |
| **ArpPing** | Zjistí MAC adresu zařízení v lokální síti přes ARP (Windows `SendARP`, Linux ARP cache jádra); funguje i při blokovaném ICMP. Jen IPv4, ne multicast/broadcast. | `<IPv4 adresa>` |
| **ARPTable** | Vypíše ARP cache systému (IP, MAC, typ dynamický/statický, rozhraní) přes Win32 API (Windows) nebo `/proc/net/arp` (Linux). | – |
| **TraceRoute** | Trasování cesty paketů ke cíli pomocí rostoucího TTL (max. 30 skoků, timeout 1 s). | `<hostname/IP>` |
| **CustomTraceroute** | Další implementace traceroute (max. 30 skoků, timeout 1 s) přes `Ping` s nastaveným TTL a zakázanou fragmentací. | `<cíl>` |
| **LatencyMonitor** | Průběžně pinguje více cílů najednou (timeout 1 s), ukončení Ctrl+C. | `<host1> [host2 ...]` |
| **TopologyMapper** | Pingem prochází všechny hosty v podsíti (po dávkách po 50) a vypíše aktivní zařízení. Prefix /1–/30. | `<síť/CIDR>`<br>př. `TopologyMapper 192.168.1.0/24` |
| **PortScanner** | TCP scan nejběžnějších portů (21, 22, 23, 25, 53, 80, 110, 143, 443, 993, 995) – otevřený / zavřený / filtrovaný. | `<hostname/IP>` |
| **ServiceFingerprinter** | Identifikuje službu na portu: u HTTP (80, 443, 8080) pošle `GET /` a vypíše odpověď (u 443 přes TLS), u ostatních čeká 3 s na banner. | `<host> <port>` |
| **HostName** | Reverzní DNS – zjistí jméno hostitele z IPv4 adresy. | `<IPv4 adresa>` |
| **NetBIOSNameResolver** | Zjistí NetBIOS jméno z IPv4 adresy – nejprve `nbtstat` (jen Windows), poté přímý dotaz NBSTAT na UDP 137. Cíl musí mít zapnutý NetBIOS over TCP/IP. | `<IPv4 adresa>` |
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
| **WifiScanner** | Skenuje WiFi sítě (Windows `netsh wlan show networks mode=bssid`, Linux `nmcli`) – SSID, signál, autentizace, kanál, BSSID. Na Windows může vyžadovat správce a zapnuté služby určování polohy. | – |
| **CDP_LLDP_Scanner** | Pasivně zachytává CDP a LLDP pakety (SharpPcap + Npcap) a zobrazí informace o sousedním přepínači (hostname, port, platforma, capabilities, chassis ID…). Adaptér se vybírá interaktivně, ENTER ukončí. | – (interaktivní výběr adaptéru) |
| **SimpleSniffer** ⚠️ | Zachytává IP pakety (ICMP/TCP/UDP…) na raw socketu (Windows, promiskuitní režim) nebo packet socketu (Linux) a vypisuje zdroj, cíl, protokol a velikost. Jen IPv4; vyžaduje správce / root. | – |

## Výpočty a konfigurace

| Program | Co dělá | Parametry |
|---|---|---|
| **IPCalculator** | Z IP a masky spočítá síťovou adresu, broadcast, rozsah použitelných adres a počet hostů. | `<IP/maska>` (maska 0–32)<br>př. `IPCalculator 192.168.1.0/24` |
| **SubnetCalculator** | Spočítá novou masku (prefix) podsítě a skutečnou kapacitu pro požadovaný počet hostů. | `<IP/maska> <počet hostů>`<br>př. `SubnetCalculator 192.168.1.0/24 50` |
| **BinaryConverter** | Převede IPv4 adresu a masku do binárního a hexadecimálního tvaru, vyznačí hranici sítě a hostů (zeleně/žlutě a řádkem S/H) a spočítá adresu sítě, broadcast a rozsah použitelných adres. Maska jako `/prefix` nebo desítkově. | `<IP/prefix>` nebo `<IP> <maska>` nebo jen `<IP>`<br>př. `BinaryConverter 192.168.1.77/26` |
| **VLSMCalculator** | Rozdělí síť na podsítě různých velikostí (VLSM): přiděluje od největšího požadavku, zarovnává na hranici podsítě a vypíše síť, masku, rozsah použitelných adres, broadcast a využití; ohlásí, co se už nevejde. | `<síť/prefix> <hostů1> [hostů2 ...]`<br>př. `VLSMCalculator 192.168.0.0/24 100 50 25 10` |
| **WildcardMaskCalculator** | Z masky spočítá wildcard masku a vypíše hotové řádky pro ACL (včetně `host`/`any`), OSPF a EIGRP `network`. Režim `wildcard` rozebere zadanou wildcard masku (počet odpovídajících adres, rozsah, u nesouvislé masky výpis adres). | `<IP/prefix>` nebo `<IP> <maska>` nebo `wildcard <IP> <wildcard>`<br>př. `WildcardMaskCalculator wildcard 192.168.1.0 0.0.0.254` |
| **IPv6Calculator** | Rozebere IPv6 adresu: plný a zkrácený tvar, typ (global, link-local, ULA, multicast…), interface ID a MAC z EUI-64, solicited-node multicast; s prefixem i síť, první/poslední adresu a počet podsítí /64. Režim `eui64` sestaví adresy z MAC. | `<IPv6[/prefix]>` nebo `eui64 <MAC> [prefix]`<br>př. `IPv6Calculator eui64 0011.2233.4455 2001:db8:acad:1::/64` |
| **SummarizationTool** | Sumarizace IPv4 sítí: najde nejmenší jedinou sumární trasu (s maskou a wildcard maskou), řekne, zda je přesná nebo kolik adres je navíc, a vypíše přesné minimální pokrytí. Zvýrazní společné bity. | `<síť1/prefix> <síť2/prefix> [další ...]`<br>př. `SummarizationTool 192.168.0.0/24 192.168.1.0/24 192.168.2.0/24 192.168.3.0/24` |
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
| **DhcpClientSimulator** | Odešle DHCP Discover (broadcast, port 67, naslouchá na 68, na Linuxu vyžaduje root; timeout 5 s) a vypíše typ odpovědi, nabízenou IP a server. | – |
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
| **SyslogServer** | Syslog server na UDP portu (výchozí **514**): rozloží hlavičku `<PRI>` na facility a závažnost, zprávy barevně vypíše a umí filtrovat podle závažnosti. Po Ctrl+C vypíše souhrn počtu zpráv podle závažnosti. Na Cisco zapnete `logging host <IP>`. | `[port=514] [max_severity=7]` (0 = Emergency … 7 = Debug)<br>př. `SyslogServer 514 4` |
| **SnmpTrapReceiver** | Přijímá SNMP trapy (v1, v2c) a informy na UDP portu (výchozí **162**) a dekóduje je včetně vazeb; zná názvy běžných OID (linkDown, ifOperStatus, sysUpTime…). SNMPv3 nepodporuje a Inform nepotvrzuje. Na Cisco `snmp-server enable traps` a `snmp-server host <IP> version 2c <community>`. | `[port=162]` |
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

Všechny projekty cílí na `net8.0` i `net10.0` (společné nastavení je v `Directory.Build.props`). Je potřeba .NET SDK 10 (umí sestavit oba cíle). Zdrojový kód je společný pro Windows i Linux, cílový systém se volí až při publikaci parametrem `-r`:

```
dotnet publish JN_console_app.slnx -c Release -f net10.0 -r win-x64
dotnet publish JN_console_app.slnx -c Release -f net10.0 -r linux-x64
```

Bez parametru `-r` se použije `win-x64`. Pro .NET 8 použijte `-f net8.0`; pro 64bitový ARM Linux (např. Raspberry Pi) `-r linux-arm64`. Linuxové programy lze sestavit i na Windows.

Výstup je v `Release\<systém>\<net8.0|net10.0>\`, např. `Release\win-x64\net10.0\PortScanner.exe` nebo `Release/linux-x64/net10.0/PortScanner`. Každý program je jeden samostatný soubor (bez doprovodných `.dll`), který vyžaduje nainstalovaný .NET Runtime příslušné verze. Na Linuxu je po stažení nebo zkopírování z Windows nutné nastavit právo ke spuštění: `chmod +x ./PortScanner`.

## Linux

Většina programů funguje na Linuxu beze změny. Programy, které používají systémově závislé funkce, mají samostatnou větev pro Linux:

| Program | Na Linuxu |
|---|---|
| **ARPTable** | čte `/proc/net/arp` |
| **ArpPing** | odešle UDP datagram, kterým jádro vyvolá ARP, a MAC přečte z `/proc/net/arp`; funguje jen pro zařízení ve stejné podsíti, root není potřeba |
| **WifiScanner** | používá `nmcli` (NetworkManager), root není potřeba |
| **SimpleSniffer** | packet socket (AF_PACKET), zachytává na všech rozhraních; vyžaduje `root` (`sudo ./SimpleSniffer`) |
| **NetBIOSNameResolver** | `nbtstat` neexistuje, použije se jen přímý UDP dotaz a DNS |
| **CDP_LLDP_Scanner** | místo Npcap potřebuje balíček `libpcap` (např. `libpcap0.8`) a práva `root` |
| **LdapBrowser** | potřebuje knihovnu `libldap` (např. `libldap-2.5-0`) |
| **Nmea0183Reader**, **Nmea2000Reader** | sériové porty se jmenují `/dev/ttyUSB0`, `/dev/ttyS0`…; uživatel musí být ve skupině `dialout` |

Další upozornění: porty pod 1024 (SyslogServer 514, SnmpTrapReceiver 162, TelnetServer 23, DNSBlackhole 53, DhcpClientSimulator 68) vyžadují na Linuxu `root` nebo `setcap 'cap_net_bind_service=+ep'`. Ping a traceroute používají ICMP; pokud systém neumožňuje ICMP bez oprávnění, použije .NET systémový příkaz `ping` (balíček `iputils-ping`).

Linuxová verze byla ověřena v prostředí WSL2 (x64) u programů ARPTable, ArpPing, WifiScanner (s ukázkovým výstupem `nmcli`), SimpleSniffer, NetBIOSNameResolver, AdvancedPing, SyslogServer, PortScanner a dalších jednoduchých nástrojů. CDP_LLDP_Scanner, LdapBrowser a čtečky NMEA na Linuxu otestovány nebyly; ověřeno také nebylo na fyzických distribucích a jiných architekturách.
