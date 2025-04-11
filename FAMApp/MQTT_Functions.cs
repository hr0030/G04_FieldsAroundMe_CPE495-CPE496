using MQTTnet.Client;
using MQTTnet;
using System.Diagnostics;
using System.Text;
using static Parse_Graph_Functions;
public class MQTT_Functions
{
    public IMqttClient _client;
    public MqttClientOptions _options;
    private Parse_Graph_Functions parse_graph;

    public MQTT_Functions(Parse_Graph_Functions parseGraph)
    {
        this.parse_graph = parseGraph;
    }

        public void MqttReceiver(string ipAddress, string subscriberTopic, string commandPayload)
    {

        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithClientId("CSHarpClient")
            .WithTcpServer(ipAddress)
            .Build();

        _client.ConnectedAsync += async e =>
        {
            Debug.WriteLine("Connected to MQTT broker.");


            var message = new MqttApplicationMessageBuilder()
                .WithTopic("desktop/commands")
                .WithPayload(commandPayload)
                .Build();

            await _client.PublishAsync(message);
            Debug.WriteLine($"Published '{commandPayload}' to 'desktop/commands'.");
            if (subscriberTopic == "Upload")
            {

            }

            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(subscriberTopic).Build());
            Debug.WriteLine($"Subscribed to topic '{subscriberTopic}'.");
        };

        _client.DisconnectedAsync += async e =>
        {
            Debug.WriteLine("Disconnected from MQTT broker.");
        };

        _client.ApplicationMessageReceivedAsync += async e =>
        {
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            payload = payload.Trim();
            Debug.WriteLine(payload);

            if (payload == "line_eof")
                parse_graph.GraphAPI("line");

            else if (payload == "lollipop_eof")
                parse_graph.GraphAPI("lollipop");

            else if (commandPayload.Contains("live"))
                parse_graph.ParseAndGraphLiveData(payload);

            else if (commandPayload.Contains("fetch_donki_gst"))
                parse_graph.ParseAPI(payload);

            else if (commandPayload.Contains("fetch"))
                parse_graph.ParseAPI(payload);

            else
                Debug.WriteLine($"Unrecognized Command: {commandPayload}");

        };
    }

    public async Task MqttSendFile(string ipAddress, string commandPayload, string filePath)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId("CSHarpClient")
            .WithTcpServer(ipAddress)
            .Build();

        await client.ConnectAsync(options);
        Debug.WriteLine("Connected to MQTT broker.");

        var commandMessage = new MqttApplicationMessageBuilder()
            .WithTopic("desktop/commands")
            .WithPayload(commandPayload)
            .Build();

        await client.PublishAsync(commandMessage);
        Debug.WriteLine($"Published command '{commandPayload}' to 'desktop/commands'.");

        Thread.Sleep(1000); // Sleep for 1 seconds

        if (System.IO.File.Exists(filePath))
        {
            foreach (var line in System.IO.File.ReadLines(filePath))
            {
                var lineMessage = new MqttApplicationMessageBuilder()
                    .WithTopic("desktop/data")
                    .WithPayload(line)
                    .Build();

                await client.PublishAsync(lineMessage);
                Debug.WriteLine($"Published line: {line}");
                // await Task.Delay(5); // Optional delay
            }
        }
        else
        {
            Debug.WriteLine("File not found: " + filePath);
            return;
        }

        var eofMessage = new MqttApplicationMessageBuilder()
            .WithTopic("desktop/data")
            .WithPayload("End of File")
            .Build();

        await client.PublishAsync(eofMessage);
        Debug.WriteLine("Published 'End of File' to indicate end of file.");

        await client.DisconnectAsync();
        Debug.WriteLine("Disconnected from MQTT broker.");
        MessageBox.Show("Notice: File has been Completely Uploaded", "End of File", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public async Task StartAsync()
    {
        try
        {
            await _client.ConnectAsync(_options);
            Debug.WriteLine("Connection attempt completed.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to connect: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        await _client.DisconnectAsync();
    }

}


