using MQTTnet.Client;
using MQTTnet;
using System.Diagnostics;
using System.Text;
using static Parse_Graph_Functions;
using System.Threading.Channels;
using MQTTnet.Protocol;
public class MQTT_Functions
{
    public IMqttClient _client;
    public MqttClientOptions _options;
    private Parse_Graph_Functions parse_graph;

    private bool isLiveSubscribed = false; // Track state

    private readonly Channel<string> messageChannel = Channel.CreateUnbounded<string>();
    private CancellationTokenSource processingCts = new CancellationTokenSource();

    public MQTT_Functions(Parse_Graph_Functions parseGraph)
    {
        this.parse_graph = parseGraph;
    }

    public async void MqttReceiver(string ipAddress, string subscriberTopic, string commandPayload)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        _options = new MqttClientOptionsBuilder()
            .WithClientId("CSharpClient")
            .WithTcpServer(ipAddress)
            .Build();

        _client.ConnectedAsync += async e =>
        {
            Debug.WriteLine("Connected to MQTT broker.");

            // Publish the command
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("desktop/commands")
                .WithPayload(commandPayload)
                .Build();

            await _client.PublishAsync(message);
            Debug.WriteLine($"Published '{commandPayload}' to 'desktop/commands'.");

            // Subscribe
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(subscriberTopic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce).Build());
            Debug.WriteLine($"Subscribed to topic '{subscriberTopic}'.");

            if ((!isLiveSubscribed) && subscriberTopic.Contains("live"))
                isLiveSubscribed = true;
        };

        _client.DisconnectedAsync += async e =>
        {
            Debug.WriteLine("Disconnected from MQTT broker.");
        };

        _client.ApplicationMessageReceivedAsync += async e =>
        {
            try
            {
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                payload = payload.Trim();

                // Enqueue the message quickly
                await messageChannel.Writer.WriteAsync(payload);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enqueueing message: {ex.Message}");
            }
        };

        try
        {
            await _client.ConnectAsync(_options);
            Debug.WriteLine("Connection attempt completed.");

            // Start processing incoming messages
            _ = ProcessMessagesAsync(commandPayload);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to connect: {ex.Message}");
        }
    }

    private async Task ProcessMessagesAsync(string commandPayload)
    {
        try
        {
            await foreach (var payload in messageChannel.Reader.ReadAllAsync(processingCts.Token))
            {
                Debug.WriteLine(payload);

                if (payload == "line_eof")
                {
                    parse_graph.GraphAPI("line");
                    await StopAsync();
                }
                else if (payload == "lollipop_eof")
                {
                    parse_graph.GraphAPI("lollipop");
                    await StopAsync();
                }
                else if (commandPayload.Contains("live"))
                {
                    parse_graph.ParseAndGraphLiveData(payload);
                }
                else if (commandPayload.Contains("fetch"))
                {
                    parse_graph.ParseAPI(payload);
                }
                else
                {
                    Debug.WriteLine($"Unrecognized payload: {payload}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Message processing canceled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error processing messages: {ex.Message}");
        }
        finally
        {
            isProcessing = false; // Reset processing state
        }
    }


    public void ResetChannel()
    {
        processingCts.Cancel(); // Cancel ongoing operations
        processingCts.Dispose();
        processingCts = new CancellationTokenSource(); // Create a new cancellation token
    }

    private bool isProcessing = false;

    public async Task StartAsync(string commandPayload)
    {
        if (isProcessing)
        {
            Debug.WriteLine("Processing is already running.");
            return;
        }

        isProcessing = true;
        try
        {
            ResetChannel();
            processingCts = new CancellationTokenSource();
            await _client.ConnectAsync(_options);
            _ = ProcessMessagesAsync(commandPayload);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start async process: {ex.Message}");
        }
        finally
        {
            isProcessing = false;
        }
    }



    public async Task StopAsync()
    {
        try
        {
            processingCts.Cancel(); // Cancel the current token
            processingCts.Dispose(); // Dispose of the old token source
            processingCts = new CancellationTokenSource(); // Create a new token source

            if (_client != null && _client.IsConnected)
            {
                await _client.DisconnectAsync();
                Debug.WriteLine("Client disconnected.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during StopAsync: {ex.Message}");
        }
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



    public async Task ToggleLiveSubscription(ToolStripButton toggleButton)
    {
        if (_client != null && _client.IsConnected)
        {
            try
            {
                if (isLiveSubscribed)
                {
                    await _client.UnsubscribeAsync("sensor/data");
                    Debug.WriteLine("Unsubscribed from 'live' topic.");
                    isLiveSubscribed = false;
                }
                else
                {
                    await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensor/data").Build());
                    Debug.WriteLine("Subscribed to 'live' topic.");
                    isLiveSubscribed = true;
                }

                // Update the button text after toggling
                UpdateToggleButton(toggleButton);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to toggle 'live' subscription: {ex.Message}");
            }
        }
        else
        {
            Debug.WriteLine("Client is not connected. Cannot toggle subscription.");
        }
    }

    private void UpdateToggleButton(ToolStripButton toggleButton)
    {
        if (isLiveSubscribed)
        {
            toggleButton.Text = "Stop Live";
            toggleButton.BackColor = Color.LightCoral;  // Optional: red-ish color
        }
        else
        {
            toggleButton.Text = "Start Live ";
            toggleButton.BackColor = Color.LightGreen;  // Optional: green-ish color
        }
    }

}

