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

namespace FAMApp
{
    public partial class Form1 : Form
    {
        private IMqttClient _client;
        private MqttClientOptions _options;
        private List<DateTime> _dates;
        private List<double> _voltages;
        // At the class level
        private List<DateTime> dates_GSC = new List<DateTime>();
        private List<double> powers_GSC = new List<double>();
        private FormsPlot formsPlot;
        private FormsPlot newAPIPlot;

        public Form1()
        {
            InitializeComponent();
            InitializeChart();
            InitializeNewAPIPlot();
        }

        private void InitializeChart()
        {
            formsPlot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(formsPlot);

            // Customize the X and Y axes
            formsPlot.Plot.Axes.DateTimeTicksBottom();
            formsPlot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            formsPlot.Plot.Axes.Left.Label.Text = "Voltage (mV)";
        }

        private void InitializeNewAPIPlot()
        {
            newAPIPlot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(newAPIPlot);

            // Customize the X and Y axes
            newAPIPlot.Plot.Axes.DateTimeTicksBottom();
            newAPIPlot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            newAPIPlot.Plot.Axes.Left.Label.Text = "Voltage (mV)";
        }

        private void sourceButton1_Click(object sender, EventArgs e)
        {
           
        }

        private void cloudToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void geomagneticStormsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string ipAddress = PromptForIPAddress();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                spawnAPIPopup();
                MqttReceiver(ipAddress, "api/data",  "fetch_donki");
                _ = StartAsync();
            }
        }

        private void spawnAPIPopup()
        {
            // Create a new Form for the pop-out window
            Form popOutForm = new Form
            {
                Text = "New Plot Window",
                Size = new Size(500, 400) 
            };

            popOutForm.Controls.Add(newAPIPlot);
            newAPIPlot.Plot.Axes.DateTimeTicksBottom();
            newAPIPlot.Plot.Axes.Bottom.Label.Text = "Date and Time";
            newAPIPlot.Plot.Axes.Left.Label.Text = "Power";
            newAPIPlot.Refresh();

            // Show the pop-out window
            popOutForm.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void wifiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string ipAddress = PromptForIPAddress();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                MqttReceiver(ipAddress, "sensor/data",  "live");
                _ = StartAsync();
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
            var dates = new List<DateTime>();
            var voltages = new List<double>();

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    reader.ReadLine(); // Skip header line

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var columns = line.Split(',');

                        if (DateTime.TryParse(columns[0], out DateTime dateTime) &&
                            double.TryParse(columns[1], out double millivolts))
                        {
                            dates.Add(dateTime);
                            voltages.Add(millivolts);
                        }
                    }
                }

                // Call the plotting function with the extracted data
                PlotData(dates, voltages, "Voltage(mV)");
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
            _dates = new List<DateTime>();
            _voltages = new List<double>();

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
            // Parse the payload (expected format: "timestamp,data")
            var parts = payload.Split(',');
            if (parts.Length == 2 &&
                DateTime.TryParse(parts[0], out DateTime timestamp) &&
                double.TryParse(parts[1], out double voltage))
            {
                _dates.Add(timestamp);
                _voltages.Add(voltage);

                // Invoke PlotData on the main thread
                formsPlot.Invoke((MethodInvoker)(() =>
                {
                    PlotData(_dates, _voltages, "Voltage(mV)");
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
            dates_GSC.Add(timestamp);
            powers_GSC.Add(averageValue);

            // Update the plot
            if (newAPIPlot != null)
            {
                if (newAPIPlot.InvokeRequired)
                {
                    newAPIPlot.Invoke((MethodInvoker)(() =>
                    {
                        PlotLollipopData(dates_GSC, powers_GSC, "KP");
                    }));
                }
                else
                {
                    PlotLollipopData(dates_GSC, powers_GSC, "KP");
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

        private void PlotData(List<DateTime> timestamps, List<double> data, string label)
        {
            try
            {
                double[] xs = timestamps.ConvertAll(date => date.ToOADate()).ToArray();
                double[] ys = data.ToArray();

                formsPlot.Plot.Clear();
                var linePlot = formsPlot.Plot.Add.Scatter(xs, ys);
                linePlot.Label = label;
                linePlot.LineWidth = 2;
                linePlot.MarkerSize = 1;

                formsPlot.Plot.Axes.AutoScale();
                formsPlot.Refresh();
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
                newAPIPlot.Plot.Clear();

                // Add lollipop sticks
                for (int i = 0; i < xs.Length; i++)
                {
                    var stick = newAPIPlot.Plot.Add.Scatter(
                        xs: new double[] { xs[i], xs[i] },
                        ys: new double[] { 0, ys[i] }
                    );
                    stick.LineWidth = 1; // Set line width
                }

                var scatterPlot = newAPIPlot.Plot.Add.Scatter(xs, ys);
                scatterPlot.Label = label;
                scatterPlot.MarkerSize = 10; 
                scatterPlot.LineStyle = ScottPlot.LineStyle.None; 

                // Auto-scale the plot and refresh
                newAPIPlot.Plot.Axes.AutoScale();
                newAPIPlot.Refresh();

                Debug.WriteLine("Lollipop graph refreshed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error plotting lollipop data: {ex.Message}");
            }
        }
    }
}



