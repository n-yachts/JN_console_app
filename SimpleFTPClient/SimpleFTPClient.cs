using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class SimpleFTPClient
{
    static async Task Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Použití: SimpleFTPClient <server> <username> <password>");
            return;
        }

        string server = args[0];
        string username = args[1];
        string password = args[2];

        try
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{server}/");
            request.Credentials = new NetworkCredential(username, password);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            using FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
            using Stream responseStream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(responseStream);

            Console.WriteLine($"Stav: {response.StatusDescription}");
            Console.WriteLine("Obsah adresáře:");

            string line = reader.ReadLine();
            while (line != null)
            {
                Console.WriteLine(line);
                line = reader.ReadLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}