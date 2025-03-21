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
using static System.Formats.Asn1.AsnWriter;
using System.Net;
using static Cloud_Functions;

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


        private void cloudToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string selectedDate = ShowSingleDatePickerDialog();
            if (string.IsNullOrEmpty(selectedDate))
            {
                MessageBox.Show("No date selected. Operation canceled.");
                return; // Exit function if no date is selected
            }
            DownloadFileFromGoogleDrive($"{selectedDate}.csv", "./");
            LoadDataFromCsv($"{selectedDate}.csv");
        }

        private void geomagneticStormsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Power", "api/data", "fetch_donki_gst");
        }

        private void Temperature_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Temperature(C)", "api/data", "fetch_temperature_api");
        }

        private void Humidity_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Percent", "api/data", "fetch_humidity_api");
        }
        private void SWIRRL_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Voltage", "api/data", "fetch_uah_swirll_api");
        }

        private void Tree_Rhythms_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Voltage", "api/data", "fetch_tree_rhythms");
        }

        private void Solar_Index_Click(object sender, EventArgs e)
        {
            getAPIGeneral("W/m^2", "api/data", "fetch_solar_index");
        }

        private void Pressure_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("mb", "api/data", "fetch_pressure_api");
        }

        private void Sunrise_Time_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Time", "api/data", "fetch_sunrise_time");
        }

        private void Sunset_Time_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Time", "api/data", "fetch_sunset_time");
        }

        private void Samsung_Watch_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("BPM", "api/data", "fetch_samsung_watch");
        }


        private Control originalParent; // Store the original parent container

        private void spawnAPIPopup(string yAxisLabel)
        {
            // Check if API_Plot already has a parent
            if (API_Plot.Parent != null)
            {
                originalParent = API_Plot.Parent; // Store the original parent
                originalParent.Controls.Remove(API_Plot); // Remove it from the parent
            }

            // Create a new Form for the pop-out window
            Form popOutForm = new Form
            {
                Text = "New Plot Window",
                Size = new Size(500, 400)
            };

            // Clear the plot before displaying the new form
            API_Plot.Plot.Clear();
            API_Plot.Refresh();

            // Add API_Plot to the new form
            popOutForm.Controls.Add(API_Plot);
            API_Plot.Dock = DockStyle.Fill;

            // Configure plot labels
            API_Plot.Plot.Axes.DateTimeTicksBottom();
            API_Plot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            API_Plot.Plot.Axes.Left.Label.Text = yAxisLabel;
            API_Plot.Refresh();

            // Handle the form closing event to restore API_Plot
            popOutForm.FormClosing += (s, e) =>
            {
                if (originalParent != null)
                {
                    originalParent.Controls.Add(API_Plot);
                    API_Plot.Dock = DockStyle.Fill; // Restore layout
                    API_Plot.Refresh();
                }
            };

            // Show the pop-out window
            popOutForm.Show();
        }


        private void getAPIGeneral(string yAxisLabel, string subscriberTopic, string commandPayload)
        {
            // Load the settings (if not already loaded)
            SettingsLoader.LoadSettings();

            // Clear Data From Plot
            API_Plot.Plot.Clear();
            API_Dates.Clear();
            API_Magnitude.Clear();

            // Get the IP address from the global variable
            string ipAddress = GlobalSettings.ServerIP;

            // Check if the IP address is not empty or null
            if (!string.IsNullOrEmpty(ipAddress))
            {
                string selectedDate = ShowDoubleDatePickerDialog();
                if (selectedDate != null)
                {
                    // Call the popup and MQTT receiver with the IP address
                    spawnAPIPopup(yAxisLabel);
                    MqttReceiver(ipAddress, subscriberTopic, commandPayload);

                    // Start the asynchronous process
                    _ = StartAsync();
                }
                else
                {
                    MessageBox.Show("Error: Problem with Selected Date", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                if (subscriberTopic == "Upload")
                {

                }
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

                if (commandPayload.Contains("live"))
                    ParseAndGraphLiveData(payload);

                else if (commandPayload.Contains("fetch_donki_gst"))
                    ParseAPILollipop(payload);

                else if (commandPayload.Contains("fetch"))
                    ParseAPILine(payload);

                else
                    Debug.WriteLine($"Unrecognized Command: {commandPayload}");

            };
        }

        private async Task MqttSendFile(string ipAddress, string commandPayload, string filePath)
        {
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId("CSHarpClient")
                .WithTcpServer(ipAddress)
                .Build();

            await client.ConnectAsync(options);
            Debug.WriteLine("Connected to MQTT broker.");

            // Send the command payload first
            var commandMessage = new MqttApplicationMessageBuilder()
                .WithTopic("desktop/commands")
                .WithPayload(commandPayload)
                .Build();

            await client.PublishAsync(commandMessage);
            Debug.WriteLine($"Published command '{commandPayload}' to 'desktop/commands'.");

            // Read file and send line by line
            if (System.IO.File.Exists(filePath))
            {
                foreach (var line in System.IO.File.ReadLines(filePath))
                {
                    var lineMessage = new MqttApplicationMessageBuilder()
                        .WithTopic("desktop/commands")
                        .WithPayload(line)
                        .Build();

                    await client.PublishAsync(lineMessage);
                    Debug.WriteLine($"Published line: {line}");
                    //await Task.Delay(100); // Optional delay to prevent flooding
                }
            }
            else
            {
                Debug.WriteLine("File not found: " + filePath);
                return;
            }

            // Send EOF to indicate the end of the file
            var eofMessage = new MqttApplicationMessageBuilder()
                .WithTopic("desktop/commands")
                .WithPayload("EOF")
                .Build();

            await client.PublishAsync(eofMessage);
            Debug.WriteLine("Published EOF to indicate end of file.");

            await client.DisconnectAsync();
            Debug.WriteLine("Disconnected from MQTT broker.");
        }


        private void ParseAndGraphLiveData(string payload)
        {
            // Parse the payload (expected format: "timestamp,channel,data")
            var parts = payload.Split(',');
            double voltage = 0; //Initialization to Avoid Errors
            int channel = 0;
            Debug.WriteLine(parts[0]);
            Debug.WriteLine(parts[1]);
            Debug.WriteLine(parts[2]);
            voltage = double.Parse(parts[2]);

            if (parts.Length == 3 &&
                DateTime.TryParse(parts[0], out DateTime timestamp) &&
                int.TryParse(parts[1], out channel) &&
                double.TryParse(parts[2], out voltage)) // Ensure channel is within range
            {
                //DateTime timestamp = DateTime.Parse(parts[0]);
                //int channel = int.Parse(parts[1]);
                //double voltage = double.Parse(parts[2]);
                // Ensure the channel exists in the dictionaries
                if (!voltagesByChannel.ContainsKey(channel))
                {
                    voltagesByChannel[channel] = new List<double>();
                }
                if (!_Dates.ContainsKey(channel))
                {
                    _Dates[channel] = new List<DateTime>();
                }
                Debug.WriteLine("Parse and Graph Live Data Reached");
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

        private void ParseAPILollipop(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                Debug.WriteLine("Payload is null or empty.");
                return;
            }

            var parts = payload.Split(',');

            // Ensure there are exactly 2 parts (timestamp and a single numerical value)
            if (parts.Length != 2)
            {
                Debug.WriteLine($"Invalid payload format: {payload}");
                return;
            }

            // Extract the timestamp from the payload
            string timestampStr = parts[0].Trim();
            if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
            {
                Debug.WriteLine($"Failed to parse timestamp: {timestampStr}");
                return;
            }

            // Extract the single numerical value
            string numericStr = parts[1].Trim();
            if (!double.TryParse(numericStr, out double numericValue))
            {
                Debug.WriteLine($"Failed to parse numerical value: {numericStr}");
                return;
            }

            // Add timestamp and numerical value to the global lists
            API_Dates.Add(timestamp);
            API_Magnitude.Add(numericValue);

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

        private void ParseAPILine(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                Debug.WriteLine("Payload is null or empty.");
                return;
            }

            var parts = payload.Split(',');

            // Ensure there are exactly 2 parts (timestamp and a single numerical value)
            if (parts.Length != 2)
            {
                Debug.WriteLine($"Invalid payload format: {payload}");
                return;
            }

            // Extract the timestamp from the payload
            string timestampStr = parts[0].Trim();
            if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
            {
                Debug.WriteLine($"Failed to parse timestamp: {timestampStr}");
                return;
            }

            // Extract the single numerical value
            string numericStr = parts[1].Trim();
            if (!double.TryParse(numericStr, out double numericValue))
            {
                Debug.WriteLine($"Failed to parse numerical value: {numericStr}");
                return;
            }

            // Add timestamp and numerical value to the global lists
            API_Dates.Add(timestamp);
            API_Magnitude.Add(numericValue);

            // Update the plot
            if (API_Plot != null)
            {
                if (API_Plot.InvokeRequired)
                {
                    API_Plot.Invoke((MethodInvoker)(() =>
                    {
                        PlotLineAPIData(API_Dates, API_Magnitude, "KP");
                    }));
                }
                else
                {
                    PlotLineAPIData(API_Dates, API_Magnitude, "KP");
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
            Debug.WriteLine("1");
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
                    Debug.WriteLine(voltagesByChannel[channel]);
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


        private void PlotLineAPIData(List<DateTime> timestamps, List<double> data, string label)
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

                Debug.WriteLine($"Plotting line graph with {xs.Length} points.");
                API_Plot.Plot.Clear();
                var linePlot = API_Plot.Plot.Add.Scatter(xs, ys);
                linePlot.LineWidth = 2;
                linePlot.MarkerSize = 1;

                // Auto-scale the plot and refresh
                API_Plot.Plot.Axes.AutoScale();
                API_Plot.Refresh();

                Debug.WriteLine("Line graph refreshed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error plotting line data: {ex.Message}");
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
            settingsForm.ShowDialog();
        }

        public static class SettingsLoader
        {
            public static void LoadSettings()
            {
                try
                {
                    string filePath = "settings.json";

                    if (System.IO.File.Exists(filePath))
                    {
                        string json = System.IO.File.ReadAllText(filePath);

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

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            genericUploadFunction("settings_upload");
        }

        private void treeRhythmsUpload_Click(object sender, EventArgs e)
        {
            genericUploadFunction("tree_rhythms_upload");
        }

        private void UAH_SWIRLL_Upload_Click(object sender, EventArgs e)
        {
            genericUploadFunction("uah_swirll_upload");
        }

        private void Upload_CSV_Cloud_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.Title = "Select a file";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    UploadFileToGoogleDrive(filePath);
                }
            }
        }
        private string ShowSingleDatePickerDialog()
        {
            using (Form dateForm = new Form())
            {
                dateForm.Text = "Date Picker";
                dateForm.Size = new Size(250, 200);
                dateForm.StartPosition = FormStartPosition.CenterScreen;

                DateTimePicker datePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Location = new Point(30, 30),
                    Width = 150
                };

                Button confirmButton = new Button
                {
                    Text = "OK",
                    Location = new Point(75, 80),
                    DialogResult = DialogResult.OK
                };

                dateForm.Controls.Add(datePicker);
                dateForm.Controls.Add(confirmButton);
                dateForm.AcceptButton = confirmButton;

                if (dateForm.ShowDialog() == DialogResult.OK)
                {
                    return datePicker.Value.ToString("yyyy_MM_dd"); // Format: d_m_y
                }
            }
            return null; // If the user cancels the selection
        }

        private string ShowDoubleDatePickerDialog()
        {
            using (Form dateForm = new Form())
            {
                dateForm.Text = "Date Picker";
                dateForm.Size = new Size(250, 200);
                dateForm.StartPosition = FormStartPosition.CenterScreen;

                DateTimePicker startDatePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Location = new Point(30, 30),
                    Width = 150
                };

                DateTimePicker endDatePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Location = new Point(30, 60),
                    Width = 150
                };

                Button confirmButton = new Button
                {
                    Text = "OK",
                    Location = new Point(75, 110),
                    DialogResult = DialogResult.OK
                };

                dateForm.Controls.Add(startDatePicker);
                dateForm.Controls.Add(endDatePicker);
                dateForm.Controls.Add(confirmButton);
                dateForm.AcceptButton = confirmButton;

                if (dateForm.ShowDialog() == DialogResult.OK)
                {
                    if (startDatePicker.Value > endDatePicker.Value)
                    {
                        MessageBox.Show("Error: End Date is Before Start Date", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                    string return_date_string = startDatePicker.Value.ToString("yyyy_MM_dd") + "," + endDatePicker.Value.ToString("yyyy_MM_dd");
                    return return_date_string; // Format: d_m_y
                }
            }
            return null; // If the user cancels the selection
        }

        private void genericUploadFunction(string commandPayload)
        {
            // Load the settings (if not already loaded)
            SettingsLoader.LoadSettings();

            // Get the IP address from the global variable
            string ipAddress = GlobalSettings.ServerIP;

            // Check if the IP address is not empty or null
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // Show date picker dialog and get the selected date
                string selectedDate = ShowSingleDatePickerDialog();
                if (string.IsNullOrEmpty(selectedDate))
                {
                    MessageBox.Show("No date selected. Operation canceled.");
                    return; // Exit function if no date is selected
                }

                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    openFileDialog.Title = "Select a file";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = openFileDialog.FileName;

                        // Modify the command payload to include the selected date
                        string updatedCommandPayload = $"{commandPayload},{selectedDate}";

                        MqttSendFile(ipAddress, updatedCommandPayload, filePath);
                    }
                }
            }
            else
            {
                // Handle the case where the IP address is not set
                MessageBox.Show("IP Address is not configured.");
            }
        }


    }
}



