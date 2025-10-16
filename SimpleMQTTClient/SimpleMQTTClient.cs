using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

class SimpleMQTTClient
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Použití: SimpleMQTTClient <broker> <topic>");
            Console.WriteLine("Příklad: SimpleMQTTClient localhost test/topic");
            return;
        }

        string broker = args[0];
        string topic = args[1];

        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker)
            .Build();

        client.ConnectedAsync += async e =>
        {
            Console.WriteLine("Připojeno k brokerovi.");
            await client.SubscribeAsync(topic);
            Console.WriteLine($"Odebíráno téma: {topic}");
        };

        client.DisconnectedAsync += async e =>
        {
            Console.WriteLine("Odpojeno od brokeru.");
            await Task.Delay(TimeSpan.FromSeconds(5));
            try
            {
                await client.ConnectAsync(options);
            }
            catch
            {
                Console.WriteLine("Připojení selhalo.");
            }
        };

        client.ApplicationMessageReceivedAsync += e =>
        {
            Console.WriteLine($"Přijata zpráva: {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}");
            return Task.CompletedTask;
        };

        try
        {
            await client.ConnectAsync(options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba připojení: {ex.Message}");
            return;
        }

        Console.WriteLine("Stiskněte Enter pro ukončení.");
        Console.ReadLine();

        await client.DisconnectAsync();
    }
}