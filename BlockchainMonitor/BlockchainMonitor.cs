using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class BlockchainMonitor
{
    static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Použití: BlockchainMonitor <rpc_url> <username> <password>");
            Console.WriteLine("Příklad: BlockchainMonitor http://127.0.0.1:8332 bitcoinrpc yourpassword");
            return;
        }

        string rpcUrl = args[0];
        string username = args[1];
        string password = args[2];

        try
        {
            Console.WriteLine("Blockchain Node Monitor\n");
            await MonitorBitcoinNode(rpcUrl, username, password);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba: {ex.Message}");
        }
    }

    static async Task MonitorBitcoinNode(string rpcUrl, string username, string password)
    {
        using (HttpClient client = new HttpClient())
        {
            // Basic autentizace
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            // Získání blockchain informací
            var blockchainInfo = await MakeRpcCall(client, rpcUrl, "getblockchaininfo");
            Console.WriteLine("=== Blockchain Info ===");
            Console.WriteLine($"Řetězec: {blockchainInfo?.chain ?? "N/A"}");
            Console.WriteLine($"Bloky: {blockchainInfo?.blocks ?? 0}");
            Console.WriteLine($"Headers: {blockchainInfo?.headers ?? 0}");
            Console.WriteLine($"Verifikováno: {blockchainInfo?.verificationprogress ?? 0:P2}");
            Console.WriteLine($"Difficulty: {blockchainInfo?.difficulty ?? 0}");

            // Získání network informací
            var networkInfo = await MakeRpcCall(client, rpcUrl, "getnetworkinfo");
            Console.WriteLine("\n=== Network Info ===");
            Console.WriteLine($"Verze: {networkInfo?.version ?? 0}");
            Console.WriteLine($"Protokol verze: {networkInfo?.protocolversion ?? 0}");
            Console.WriteLine($"Connections: {networkInfo?.connections ?? 0}");

            // Získání mining informací
            var miningInfo = await MakeRpcCall(client, rpcUrl, "getmininginfo");
            Console.WriteLine("\n=== Mining Info ===");
            Console.WriteLine($"Pooled transactions: {miningInfo?.pooledtx ?? 0}");
            Console.WriteLine($"Difficulty: {miningInfo?.difficulty ?? 0}");
            Console.WriteLine($"Network Hashrate: {miningInfo?.networkhashps ?? 0}");

            // Získání mempool info
            var mempoolInfo = await MakeRpcCall(client, rpcUrl, "getmempoolinfo");
            Console.WriteLine("\n=== Mempool Info ===");
            Console.WriteLine($"Počet transakcí: {mempoolInfo?.size ?? 0}");
            Console.WriteLine($"Velikost: {mempoolInfo?.bytes ?? 0} bytes");
        }
    }

    static async Task<dynamic> MakeRpcCall(HttpClient client, string rpcUrl, string method)
    {
        var request = new
        {
            jsonrpc = "1.0",
            id = "monitor",
            method = method,
            @params = new object[] { }
        };

        string json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(rpcUrl, content);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<JsonRpcResponse>(responseContent);
            return result.result;
        }
        else
        {
            Console.WriteLine($"RPC Chyba: {responseContent}");
            return null;
        }
    }
}

class JsonRpcResponse
{
    public string result { get; set; }
    public object error { get; set; }
    public string id { get; set; }
}