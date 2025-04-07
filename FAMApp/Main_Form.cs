using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
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
using static Parse_Graph_Functions;
using static Popup_Functions;
using static MQTT_Functions;
using static Cloud_Functions;

namespace FAMApp
{
    public partial class Main_Form : Form
    {

        public FormsPlot Main_Plot;
        public FormsPlot API_Plot;

       
        Popup_Functions popups;
        Parse_Graph_Functions parse_graph;
        MQTT_Functions mqtt;

        public Main_Form()
        {
            InitializeComponent();
            InitializeChart();
            InitializeNewAPIPlot();
            popups = new Popup_Functions(Main_Plot, API_Plot);
            parse_graph = new Parse_Graph_Functions(Main_Plot, API_Plot);
            mqtt = new MQTT_Functions(parse_graph);
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
            string selectedDate = popups.ShowSingleDatePickerDialog();
            if (string.IsNullOrEmpty(selectedDate))
            {
                MessageBox.Show("No date selected. Operation canceled.");
                return; // Exit function if no date is selected
            }
            DownloadFileFromGoogleDrive($"{selectedDate}.csv", "./");
            parse_graph.LoadDataFromCsv($"{selectedDate}.csv");
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

        private void getAPIGeneral(string yAxisLabel, string subscriberTopic, string commandPayload)
        {
            // Load the settings (if not already loaded)
            SettingsLoader.LoadSettings();

            // Clear Data From Plot
            API_Plot.Plot.Clear();
            parse_graph.API_Dates.Clear();
            parse_graph.API_Magnitude.Clear();

            // Get the IP address from the global variable
            string ipAddress = GlobalSettings.ServerIP;

            // Check if the IP address is not empty or null
            if (!string.IsNullOrEmpty(ipAddress))
            {
                string selectedDate = popups.ShowDoubleDatePickerDialog();
                if (selectedDate != null)
                {
                    string updatedCommandPayload = $"{commandPayload},{selectedDate}";
                    // Call the popup and MQTT receiver with the IP address
                    popups.spawnAPIPopup(yAxisLabel);
                    mqtt.MqttReceiver(ipAddress, subscriberTopic, updatedCommandPayload);

                    // Start the asynchronous process
                    _ = mqtt.StartAsync();
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
                mqtt.MqttReceiver(ipAddress, "sensor/data", "live");
                _ = mqtt.StartAsync();
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
                    parse_graph.LoadDataFromCsv(filePath);
                }
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
            // Load the settings (if not already loaded)
            SettingsLoader.LoadSettings();

            // Get the IP address from the global variable
            string ipAddress = GlobalSettings.ServerIP;

            // Check if the IP address is not empty or null
            if (!string.IsNullOrEmpty(ipAddress))
            {
                mqtt.MqttSendFile(ipAddress, "settings_upload", "settings.json");
            }
            else
            {
                MessageBox.Show("Error: Ip Address is Undefined", "IP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

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
                else
                {
                    MessageBox.Show("Error: File is Undefined", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
                string selectedDate = popups.ShowSingleDatePickerDialog();
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

                        mqtt.MqttSendFile(ipAddress, updatedCommandPayload, filePath);
                    }
                }
            }
            else
            {
                MessageBox.Show("Error: Ip Address is Undefined", "IP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}



