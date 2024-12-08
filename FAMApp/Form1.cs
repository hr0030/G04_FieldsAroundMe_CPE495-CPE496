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

namespace FAMApp
{
    public partial class Form1 : Form
    {
        private IMqttClient _client;
        private MqttClientOptions _options;
        private List<DateTime> _dates;
        private List<double> _voltages;
        private FormsPlot formsPlot;
        
        public Form1()
        {
            InitializeComponent();
            InitializeChart();
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

        private void sourceButton1_Click(object sender, EventArgs e)
        {

        }

        private void cloudToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void wifiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string ipAddress = PromptForIPAddress();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                MqttReceiver(ipAddress);
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

                Label label = new Label() { Left = 10, Top = 20, Text = "IP Address:" };
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

        private void MqttReceiver(string ipAddress)
        {
            Debug.WriteLine("Checkpoint 1: MQTT Receiver");
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
                    .WithPayload("live")
                    .Build();

                await _client.PublishAsync(message);
                Debug.WriteLine("Published 'live' to 'desktop/commands'.");

                // Subscribe to the "sensor/data" topic
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensor/data").Build());
                Debug.WriteLine("Subscribed to topic 'sensor/data'.");
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
                else
                {
                    Debug.WriteLine($"Failed to parse payload: {payload}");
                }
            };
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
    }
}



