using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using MQTTnet;
using MQTTnet.Client;
using ScottPlot.WinForms;
using System.Threading.Tasks;
using ScottPlot.Colormaps;
using ScottPlot;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Emit;
using ScottPlot.Plottables;
using static ScottPlot.Generate;
using DateTime = System.DateTime;
using System.Threading.Channels;
using static FAMApp.Settings_Form;

namespace FAMApp
{
    public partial class Main_Form : Form
    {

        private IMqttClient _client;
        private MqttClientOptions _options;
        // Declare voltagesByChannel as a dictionary to store voltage data for each channel
        private Dictionary<int, List<double>> voltagesByChannel = new Dictionary<int, List<double>>();
        private Dictionary<int, List<DateTime>> _Dates = new Dictionary<int, List<DateTime>>();
        // At the class level
        private List<DateTime> API_Dates = new List<DateTime>();
        private List<double> API_Magnitude = new List<double>();
        private FormsPlot Main_Plot;
        private FormsPlot API_Plot;

        public Main_Form()
        {
            InitializeComponent();
            InitializeChart();
            InitializeNewAPIPlot();
        }

        public static class GlobalSettings
        {
            public static string ServerIP { get; set; }
        }

        private void InitializeChart()
        {
            Main_Plot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(Main_Plot);

            // Customize the X and Y axes
            Main_Plot.Plot.Axes.DateTimeTicksBottom();
            Main_Plot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            Main_Plot.Plot.Axes.Left.Label.Text = "Voltage (mV)";
        }

        private void InitializeNewAPIPlot()
        {
            API_Plot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(API_Plot);

            // Customize the X and Y axes
            API_Plot.Plot.Axes.DateTimeTicksBottom();
            API_Plot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            API_Plot.Plot.Axes.Left.Label.Text = "Voltage (mV)";
        }

        private void sourceButton1_Click(object sender, EventArgs e)
        {

        }

        private void cloudToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void geomagneticStormsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Power", "api/data", "fetch_donki");
        }


        private void spawnAPIPopup(string yAxisLabel)
        {
            // Create a new Form for the pop-out window
            Form popOutForm = new Form
            {
                Text = "New Plot Window",
                Size = new Size(500, 400)
            };

            popOutForm.Controls.Add(API_Plot);
            API_Plot.Plot.Axes.DateTimeTicksBottom();
            API_Plot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            API_Plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            API_Plot.Refresh();

            // Show the pop-out window
            popOutForm.Show();
        }

        private void getAPIGeneral(string yAxisLabel, string subscriberTopic, string commandPayload)
        {
            // Load the settings (if not already loaded)
            SettingsLoader.LoadSettings();

            // Get the IP address from the global variable
            string ipAddress = GlobalSettings.ServerIP;

            // Check if the IP address is not empty or null
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // Call the popup and MQTT receiver with the IP address
                spawnAPIPopup(yAxisLabel);
                MqttReceiver(ipAddress, subscriberTopic, commandPayload);

                // Start the asynchronous process
                _ = StartAsync();
            }
            else
            {
                // Handle the case where the IP address is not set
                MessageBox.Show("IP Address is not configured.");
            }
        }
        private void Main_Form_Load(object sender, EventArgs e)
        {

        }

        private void wifiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Load the settings (if not already loaded)
            SettingsLoader.LoadSettings();

            // Get the IP address from the global variable
            string ipAddress = GlobalSettings.ServerIP;
            if (!string.IsNullOrEmpty(ipAddress))
            {
                MqttReceiver(ipAddress, "sensor/data", "live");
                _ = StartAsync();
            }
            else
            {
                // Handle the case where the IP address is not set
                MessageBox.Show("IP Address is not configured.");
            }
        }

        private void microSDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                openFileDialog.Title = "Select a CSV file";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    LoadDataFromCsv(filePath);
                }
            }
        }

        private void LoadDataFromCsv(string filePath)
        {
            // Dictionary to hold separate lists for each channel (1-4)
            var voltagesByChannel = new Dictionary<int, List<double>>
{
    { 1, new List<double>() },
    { 2, new List<double>() },
    { 3, new List<double>() },
    { 4, new List<double>() }
};

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    reader.ReadLine(); // Skip header line

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var columns = line.Split(',');

                        if (columns.Length >= 3 &&
                            DateTime.TryParse(columns[0], out DateTime dateTime) &&
                            int.TryParse(columns[1], out int channel) &&
                            double.TryParse(columns[2], out double millivolts) &&
                            channel >= 1 && channel <= 4) // Ensure valid channel range
                        {
                            // Ensure the channel exists in _Dates
                            if (!_Dates.ContainsKey(channel))
                            {
                                _Dates[channel] = new List<DateTime>();
                            }

                            _Dates[channel].Add(dateTime);
                            voltagesByChannel[channel].Add(millivolts);
                        }
                    }
                }

                // Call the plotting function for each channel
                Main_Plot.Invoke((MethodInvoker)(() =>
                {
                    PlotData(voltagesByChannel, _Dates);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading CSV data: {ex.Message}");
            }
        }



        private string PromptForIPAddress()
        {
            using (Form inputForm = new Form())
            {
                inputForm.Width = 300;
                inputForm.Height = 150;
                inputForm.Text = "Enter IP Address";

                System.Windows.Forms.Label label = new System.Windows.Forms.Label() { Left = 10, Top = 20, Text = "IP Address:" };
                TextBox textBox = new TextBox() { Left = 100, Top = 20, Width = 150 };
                Button confirmation = new Button() { Text = "OK", Left = 100, Width = 100, Top = 60, DialogResult = DialogResult.OK };
                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(confirmation);
                inputForm.AcceptButton = confirmation;
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    return textBox.Text;
                }
            }
            return null;
        }

        private void MqttReceiver(string ipAddress, string subscriberTopic, string commandPayload)
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

                // Publish "live" to the "desktop/commands" topic
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("desktop/commands")
                    .WithPayload(commandPayload)
                    .Build();

                await _client.PublishAsync(message);
                Debug.WriteLine("Published '{commandPayload}' to 'desktop/commands'.");

                // Subscribe to the subscriberTopic topic
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(subscriberTopic).Build());
                Debug.WriteLine("Subscribed to topic '{subscriberTopic}'.");
            };

            _client.DisconnectedAsync += async e =>
            {
                Debug.WriteLine("Disconnected from MQTT broker.");
                // Error Handling Logic here
            };

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Debug.WriteLine("Message received event triggered");
                switch (commandPayload)
                {
                    case "live":
                        ParseAndGraphLiveData(payload);
                        break;

                    case "fetch_donki":
                        ParseDonki(payload);
                        break;

                    default:
                        Debug.WriteLine($"Unrecognized Command: {commandPayload}");
                        break;
                }
            };
        }




        private void ParseAndGraphLiveData(string payload)
        {
            // Parse the payload (expected format: "timestamp,channel,data")
            var parts = payload.Split(',');

            double voltage = 0; //Initialization to Avoid Errors
            int channel = 0;

            if (parts.Length == 3 &&
                DateTime.TryParse(parts[0], out DateTime timestamp) &&
                int.TryParse(parts[1], out channel) &&
                double.TryParse(parts[2], out voltage) &&
                channel >= 1 && channel <= 4) // Ensure channel is within range
            {
                // Ensure the channel exists in the dictionaries
                if (!voltagesByChannel.ContainsKey(channel))
                {
                    voltagesByChannel[channel] = new List<double>();
                }
                if (!_Dates.ContainsKey(channel))
                {
                    _Dates[channel] = new List<DateTime>();
                }

                // Add timestamp to the respective channel without checking for duplicates
                _Dates[channel].Add(timestamp);

                // Add voltage data to the corresponding channel list
                voltagesByChannel[channel].Add(voltage);

                // Invoke PlotData on the main thread
                Main_Plot.Invoke((MethodInvoker)(() =>
                {
                    PlotData(voltagesByChannel, _Dates);
                }));
            }
        }

        private void ParseDonki(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                Debug.WriteLine("Payload is null or empty.");
                return;
            }

            var parts = payload.Split(',');

            // Ensure there are at least 4 parts
            if (parts.Length < 4)
            {
                Debug.WriteLine($"Invalid payload: {payload}");
                return;
            }

            // Extract the timestamp from the payload
            string timestampStr = parts[1].Trim();
            if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
            {
                Debug.WriteLine($"Failed to parse timestamp: {timestampStr}");
                return;
            }

            // Extract numerical data from the payload
            var numericValues = parts.Skip(2)
                                     .TakeWhile(p => double.TryParse(p.Trim(), out _))
                                     .Select(p => double.Parse(p.Trim()))
                                     .ToList();

            if (numericValues.Count == 0)
            {
                Debug.WriteLine($"No numerical data found in payload: {payload}");
                return;
            }

            // Calculate the average of the numerical values
            double averageValue = numericValues.Average();

            // Add timestamp and average value to the global lists
            API_Dates.Add(timestamp);
            API_Magnitude.Add(averageValue);

            // Update the plot
            if (API_Plot != null)
            {
                if (API_Plot.InvokeRequired)
                {
                    API_Plot.Invoke((MethodInvoker)(() =>
                    {
                        PlotLollipopData(API_Dates, API_Magnitude, "KP");
                    }));
                }
                else
                {
                    PlotLollipopData(API_Dates, API_Magnitude, "KP");
                }
            }
            else
            {
                Debug.WriteLine("newAPIPlot is null. Unable to update plot.");
            }
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

        private void PlotData(Dictionary<int, List<double>> voltagesByChannel, Dictionary<int, List<DateTime>> timestamps)
        {
            try
            {
                Main_Plot.Plot.Clear(); // Clear previous plots

                IPalette palette = new ScottPlot.Palettes.Category10();

                int colorIndex = 0;
                foreach (var channel in voltagesByChannel.Keys)
                {
                    if (voltagesByChannel[channel].Count == 0 || !timestamps.ContainsKey(channel) || timestamps[channel].Count == 0)
                        continue; // Skip empty channels or channels without timestamps

                    double[] xs = timestamps[channel].Select(date => date.ToOADate()).ToArray(); // Get timestamps for the current channel
                    double[] ys = voltagesByChannel[channel].ToArray(); // Get voltages for the current channel

                    var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

                    linePlot.Label = $"Channel {channel}";
                    linePlot.LineWidth = 2;
                    linePlot.MarkerSize = 1;
                    linePlot.Color = palette.GetColor(colorIndex++); // Get a unique color
                }

                Main_Plot.Plot.Legend.IsVisible = true; // Show legend to differentiate channels
                Main_Plot.Plot.Axes.AutoScale(); // Auto-scale for better visibility
                Main_Plot.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error plotting data: {ex.Message}");
            }
        }



        private void PlotLollipopData(List<DateTime> timestamps, List<double> data, string label)
        {
            try
            {
                if (timestamps == null || data == null || timestamps.Count == 0 || data.Count == 0)
                {
                    MessageBox.Show("No data to plot. Please ensure timestamps and data are populated.");
                    return;
                }

                // Convert DateTime to OADate for plotting
                double[] xs = timestamps.ConvertAll(date => date.ToOADate()).ToArray();
                double[] ys = data.ToArray();

                if (xs.Length != ys.Length)
                {
                    MessageBox.Show("Mismatch between timestamps and data lengths.");
                    return;
                }

                Debug.WriteLine($"Plotting lollipop graph with {xs.Length} points.");
                API_Plot.Plot.Clear();

                // Add lollipop sticks
                for (int i = 0; i < xs.Length; i++)
                {
                    var stick = API_Plot.Plot.Add.Scatter(
                        xs: new double[] { xs[i], xs[i] },
                        ys: new double[] { 0, ys[i] }
                    );
                    stick.LineWidth = 1; // Set line width
                }

                var scatterPlot = API_Plot.Plot.Add.Scatter(xs, ys);
                scatterPlot.Label = label;
                scatterPlot.MarkerSize = 10;
                scatterPlot.LineStyle = ScottPlot.LineStyle.None;

                // Auto-scale the plot and refresh
                API_Plot.Plot.Axes.AutoScale();
                API_Plot.Refresh();

                Debug.WriteLine("Lollipop graph refreshed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error plotting lollipop data: {ex.Message}");
            }
        }

        private void Settings_Button_Click(object sender, EventArgs e)
        {
            // Create an instance of the Settings_Form
            Settings_Form settingsForm = new Settings_Form();

            // Show the form as a modal dialog (blocks the main form until the settings form is closed)
            settingsForm.ShowDialog();

            // If you want to show the form non-modally (allows interaction with both forms):
            // settingsForm.Show();
        }

        public static class SettingsLoader
        {
            public static void LoadSettings()
            {
                try
                {
                    string filePath = "settings.json";

                    if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);

                        // Deserialize the JSON into a Settings object
                        Settings settings = JsonConvert.DeserializeObject<Settings>(json);

                        // Set the global ServerIP to the value read from the JSON
                        GlobalSettings.ServerIP = settings.ServerIP;
                    }
                    else
                    {
                        MessageBox.Show("Settings file not found. Using default IP.");
                        // Set a default IP if settings are not found
                        GlobalSettings.ServerIP = "192.168.1.1";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading settings: {ex.Message}");
                }
            }
        }

    }
}



