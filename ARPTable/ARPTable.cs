using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

class ARPTable
{
    static void Main()
    {
        Console.WriteLine("ARP Table - Lokální cache\n");

        try
        {
            var arpProcess = new Process();
            arpProcess.StartInfo.FileName = "arp";
            arpProcess.StartInfo.Arguments = "-a";
            arpProcess.StartInfo.UseShellExecute = false;
            arpProcess.StartInfo.RedirectStandardOutput = true;
            arpProcess.StartInfo.CreateNoWindow = true;

            arpProcess.Start();
            string output = arpProcess.StandardOutput.ReadToEnd();
            arpProcess.WaitForExit();

            Console.WriteLine(output);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }
}