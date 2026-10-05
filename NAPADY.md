# Nápady na další programy (CCNA)

Seznam nápadů na nové konzolové nástroje pro studenty Cisco CCNA. Existující nástroje (ping, traceroute, SNMP walk, DHCP, CDP/LLDP, NTP, ARP, Telnet) zde nejsou.

## Vybráno k realizaci

- [x] **BinaryConverter** – zobrazí IP adresu a masku binárně a vyznačí hranici sítě
- [x] **SyslogServer** – přijímá zprávy na UDP 514, filtruje podle severity (0–7) a zobrazuje její název
- [x] **SnmpTrapReceiver** – přijímá SNMP trapy na UDP 162 (protějšek SnmpWalkeru)

## Síťová vrstva a adresování

- [x] **VLSMCalculator** – rozdělí síť na podsítě různých velikostí, např. `10.0.0.0/24 100 50 25 10` (navazuje na SubnetCalculator)
- [x] **IPv6Calculator** – prefixy, zkracování adres, rozpoznání typu (link-local, ULA, global), EUI-64 z MAC
- [x] **WildcardMaskCalculator** – převod mezi maskou a wildcard maskou pro ACL, případně rovnou řádek `access-list`
- [x] **SummarizationTool** – sloučí seznam sítí do co nejmenší sumarizované trasy

## Směrování a přepínání (simulace principů)

- **RouteLookupSimulator** – načte směrovací tabulku ze souboru a ukáže, kterou trasu router vybere (longest prefix match, AD, metrika)
- **MacTableSimulator** – učení MAC adres, flooding a forwarding na přepínači
- **STPElection** – výběr root bridge a stavů portů ze zadaných priorit a cen linek
- **OspfCostCalculator** – cena z referenční šířky pásma a výběr nejlepší cesty

## Zabezpečení a služby

- **AclTester** – otestuje, zda paket (zdroj, cíl, protokol, port) projde zadaným ACL
- **NatSimulator** – statický NAT, dynamický NAT a PAT včetně překladové tabulky
- **TftpServer** / **TftpClient** – zálohování konfigurace a IOS

## Přístup k zařízením

- **SshClient** – protějšek TelnetClientu (např. Renci.SshNet)
- **ConfigBackup** – projde seznam zařízení a uloží výstup `show running-config`
- **SerialConsole** – jednoduchý terminál přes COM port ke konzoli Cisco (9600 8N1)

## Diagnostika

- **MtuDiscovery** – zjištění MTU cesty pingem s příznakem Don't Fragment
- **DnsQueryTool** – záznamy A, AAAA, MX, NS, PTR, TXT (DNSResolver umí jen A/AAAA)
- **VlanFrameParser** – načte `.pcap` a zobrazí 802.1Q tagy a Ethernet hlavičky
