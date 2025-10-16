using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

class ContainerNetworkInspector
{
    static async Task Main()
    {
        Console.WriteLine("Container Network Inspector\n");

        if (!await IsDockerAvailable())
        {
            Console.WriteLine("Docker není dostupný. Ujistěte se, že je Docker nainstalován a běží.");
            return;
        }

        await InspectDockerNetworks();
        await InspectRunningContainers();
    }

    static async Task<bool> IsDockerAvailable()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "version --format json",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }

    static async Task InspectDockerNetworks()
    {
        Console.WriteLine("=== DOCKER NETWORKS ===\n");

        try
        {
            // Získání seznamu sítí
            string networksJson = await RunDockerCommand("network ls --format json");
            string[] networkLines = networksJson.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in networkLines)
            {
                try
                {
                    var network = JsonSerializer.Deserialize<DockerNetwork>(line);
                    Console.WriteLine($"🔸 {network.Name} ({network.ID})");
                    Console.WriteLine($"   Driver: {network.Driver}");
                    Console.WriteLine($"   Scope: {network.Scope}");

                    // Detailní informace o síti
                    string detailJson = await RunDockerCommand($"network inspect {network.ID}");
                    var details = JsonSerializer.Deserialize<DockerNetworkDetail[]>(detailJson);

                    if (details != null && details.Length > 0)
                    {
                        var detail = details[0];
                        Console.WriteLine($"   Subnet: {detail.IPAM?.Config?[0]?.Subnet ?? "N/A"}");
                        Console.WriteLine($"   Gateway: {detail.IPAM?.Config?[0]?.Gateway ?? "N/A"}");

                        if (detail.Containers != null)
                        {
                            Console.WriteLine($"   Containers: {detail.Containers.Count}");
                            foreach (var container in detail.Containers)
                            {
                                Console.WriteLine($"     - {container.Value.Name} ({container.Value.IPv4Address})");
                            }
                        }
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při parsování: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static async Task InspectRunningContainers()
    {
        Console.WriteLine("=== RUNNING CONTAINERS ===\n");

        try
        {
            string containersJson = await RunDockerCommand("ps --format json");
            string[] containerLines = containersJson.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in containerLines)
            {
                try
                {
                    var container = JsonSerializer.Deserialize<DockerContainer>(line);
                    Console.WriteLine($"🐳 {container.Names} ({container.Image})");
                    Console.WriteLine($"   ID: {container.ID}");
                    Console.WriteLine($"   Status: {container.Status}");
                    Console.WriteLine($"   Ports: {container.Ports}");

                    // Síťová nastavení kontejneru
                    string inspectJson = await RunDockerCommand($"inspect {container.ID}");
                    var details = JsonSerializer.Deserialize<DockerContainerDetail[]>(inspectJson);

                    if (details != null && details.Length > 0)
                    {
                        var detail = details[0];
                        if (detail.NetworkSettings?.Networks != null)
                        {
                            foreach (var network in detail.NetworkSettings.Networks)
                            {
                                Console.WriteLine($"   Network {network.Key}: {network.Value.IPAddress}");
                            }
                        }
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při parsování: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static async Task<string> RunDockerCommand(string arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = startInfo })
        {
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error) && process.ExitCode != 0)
                throw new Exception(error);

            return output;
        }
    }
}

// Model classes for JSON deserialization
class DockerNetwork
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Driver { get; set; }
    public string Scope { get; set; }
}

class DockerNetworkDetail
{
    public string Name { get; set; }
    public IPAM IPAM { get; set; }
    public Dictionary<string, ContainerInfo> Containers { get; set; }
}

class IPAM
{
    public List<IPAMConfig> Config { get; set; }
}

class IPAMConfig
{
    public string Subnet { get; set; }
    public string Gateway { get; set; }
}

class ContainerInfo
{
    public string Name { get; set; }
    public string IPv4Address { get; set; }
}

class DockerContainer
{
    public string ID { get; set; }
    public string Image { get; set; }
    public string Names { get; set; }
    public string Status { get; set; }
    public string Ports { get; set; }
}

class DockerContainerDetail
{
    public NetworkSettings NetworkSettings { get; set; }
}

class NetworkSettings
{
    public Dictionary<string, NetworkInfo> Networks { get; set; }
}

class NetworkInfo
{
    public string IPAddress { get; set; }
    public string Gateway { get; set; }
}