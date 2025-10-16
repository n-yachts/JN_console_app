using System;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.Protocols;
using System.Net;

class LdapBrowser
{
    static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Použití: LdapBrowser <server> <port> <username> <password> [searchBase]");
            Console.WriteLine("Příklad: LdapBrowser ldap.company.com 389 cn=admin,dc=company,dc=com password dc=company,dc=com");
            return;
        }

        string server = args[0];
        int port = int.Parse(args[1]);
        string username = args[2];
        string password = args[3];
        string searchBase = args.Length > 4 ? args[4] : "";

        try
        {
            LdapDirectoryIdentifier identifier = new LdapDirectoryIdentifier(server, port);
            using (LdapConnection connection = new LdapConnection(identifier))
            {
                connection.Credential = new NetworkCredential(username, password);
                connection.AuthType = AuthType.Basic;
                connection.SessionOptions.ProtocolVersion = 3;

                Console.WriteLine($"Připojování k LDAP serveru {server}:{port}...");
                connection.Bind();
                Console.WriteLine("✅ Připojení úspěšné\n");

                // Vyhledávání objektů
                SearchRequest request = new SearchRequest(
                    searchBase,
                    "(objectClass=*)",
                    SearchScope.Subtree,
                    null
                );

                SearchResponse response = (SearchResponse)connection.SendRequest(request);

                Console.WriteLine($"Nalezeno {response.Entries.Count} objektů:\n");

                foreach (SearchResultEntry entry in response.Entries)
                {
                    Console.WriteLine($"DN: {entry.DistinguishedName}");
                    Console.WriteLine("Atributy:");

                    foreach (string attributeName in entry.Attributes.AttributeNames)
                    {
                        DirectoryAttribute attribute = entry.Attributes[attributeName];
                        Console.Write($"  {attributeName}: ");

                        foreach (object value in attribute)
                        {
                            Console.Write($"{value} ");
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine();
                }
            }
        }
        catch (LdapException ex)
        {
            Console.WriteLine($"LDAP Chyba: {ex.Message} (Error code: {ex.ErrorCode})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}