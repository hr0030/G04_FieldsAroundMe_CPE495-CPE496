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
using System.Globalization;

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
            sensorSelectionComboBox.SelectedIndex = 0;
            parse_graph = new Parse_Graph_Functions(Main_Plot, API_Plot);
            popups = new Popup_Functions(Main_Plot, API_Plot, parse_graph);
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
            string selectedSensor = sensorSelectionComboBox.SelectedItem?.ToString();
            string returnDateString = popups.ShowDoubleDatePickerDialog();
            if (string.IsNullOrEmpty(returnDateString))
            {
                MessageBox.Show("No date range selected. Operation canceled.");
                return; // Exit if user cancels
            }

            string[] dateRange = returnDateString.Split(',');
            if (dateRange.Length != 2)
            {
                MessageBox.Show("Invalid date range format.");
                return;
            }

            string startDate = dateRange[0];
            string endDate = dateRange[1];

            // Optional: Convert to DateTime objects if you want to validate or loop
            DateTime start = DateTime.ParseExact(startDate, "yyyy_MM_dd", null);
            DateTime end = DateTime.ParseExact(endDate, "yyyy_MM_dd", null);

            // Loop through each date in the range
            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                string dateString = date.ToString("yyyy_MM_dd");
                string fileName = $"{selectedSensor}_{dateString}.csv";

                DownloadFileFromGoogleDrive(fileName, "./");
                parse_graph.LoadDataFromCsv(fileName);
            }

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
        private void Samsung_Watch_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("BPM", "api/data", "fetch_samsung_watch");
        }

        private void solarFlareAPI_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Temperature(C)", "api/data", "fetch_temperature_api"); // Fix This
        }

        private void oura_Ring_HR_Reserve_Click(object sender, EventArgs e)
        {
            getHealthAPIGeneral("Heart Rate Reserve", "fetch_oura_ring,2");
        }

        private void oura_Ring_RR_Click(object sender, EventArgs e)
        {
            getHealthAPIGeneral("RR", "fetch_oura_ring,3");
        }

        private void oura_Ring_Click(object sender, EventArgs e)
        {
            getHealthAPIGeneral("Heart Rate Variability", "fetch_oura_ring,4");
        }

        private void Moon_Phase_Click(object sender, EventArgs e)
        {
            API_Plot.Plot.Clear();
            parse_graph.API_Dates.Clear();
            parse_graph.API_Magnitude.Clear();
            string selectedDate = popups.ShowDoubleDatePickerDialog();
            Debug.WriteLine(selectedDate);
            var parts = selectedDate.Split(',');

            if (parts.Length == 2 &&
    DateTime.TryParseExact(parts[0], "yyyy_MM_dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startDate) &&
    DateTime.TryParseExact(parts[1], "yyyy_MM_dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endDate))

            {
                popups.spawnAPIPopup("Moon Phase(Percent)");
                parse_graph.GenerateMoonPhaseData(startDate, endDate);
            }
            else
            {
                MessageBox.Show("Error: Problem with Selected Date", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Sun_Rise_Set_API_Click(object sender, EventArgs e)
        {
            getAPIGeneral("Time", "api/data", "fetch_sun_times");
        }

        private void getHealthAPIGeneral(string yAxisLabel, string commandPayload)
        {
            SettingsLoader.LoadSettings(); // Load the settings

            API_Plot.Plot.Clear();
            parse_graph.API_Dates.Clear();
            parse_graph.API_Magnitude.Clear();


            string ipAddress = GlobalSettings.ServerIP; // Get the IP address from settings

            if (!string.IsNullOrEmpty(ipAddress))
            {
                bool login = spawnLoginPopup();
                if (login)
                {
                    popups.spawnAPIPopup(yAxisLabel);
                    mqtt.MqttReceiver(ipAddress, "api/data", commandPayload);
                }
                else
                {
                    MessageBox.Show("Error: Incorrect Login Information", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("IP Address is not configured.");
            }
        }



        private void getAPIGeneral(string yAxisLabel, string subscriberTopic, string commandPayload)
        {
            SettingsLoader.LoadSettings(); // Load the settings

            API_Plot.Plot.Clear();
            parse_graph.API_Dates.Clear();
            parse_graph.API_Magnitude.Clear();


            string ipAddress = GlobalSettings.ServerIP; // Get the IP address from settings

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string selectedDate = popups.ShowDoubleDatePickerDialog();
                if (selectedDate != null)
                {
                    string updatedCommandPayload = $"{commandPayload},{selectedDate}";
                    bool login = spawnLoginPopup();
                    if (login)
                    {
                        popups.spawnAPIPopup(yAxisLabel);
                        mqtt.MqttReceiver(ipAddress, subscriberTopic, updatedCommandPayload);
                    }
                    else
                    {
                        MessageBox.Show("Error: Incorrect Login Information", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Error: Problem with Selected Date", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("IP Address is not configured.");
            }
        }
        private void Main_Form_Load(object sender, EventArgs e)
        {

        }

        private void wifiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsLoader.LoadSettings(); // Load the settings
            string selectedSensor = sensorSelectionComboBox.SelectedItem?.ToString();
            string ipAddress = GlobalSettings.ServerIP; // Get the IP address from settings
            parse_graph.ClearAllData();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                mqtt.MqttReceiver(ipAddress, "sensor/data", $"live,{selectedSensor}");
            }
            else
            {
                MessageBox.Show("IP Address is not configured.");
            }
        }

        // This Function is used to graph .CSVs downloaded to the Computer. 
        // Simply opens file explorer window and passes file to LoadDataFromCSV function 

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


        // Boot up Settings_Form when the settings button is pressed
        private void Settings_Button_Click(object sender, EventArgs e)
        {
            Settings_Form settingsForm = new Settings_Form();
            settingsForm.ShowDialog();
        }


        // Commonly used function that reads in settings from settings.json. Currently only reads the IP Address
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
                        Settings settings = JsonConvert.DeserializeObject<Settings>(json);
                        GlobalSettings.ServerIP = settings.ServerIP;
                    }
                    else
                    {
                        MessageBox.Show("Settings file not found. Using default IP.");
                        GlobalSettings.ServerIP = "192.168.1.1";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading settings: {ex.Message}");
                }
            }
        }



        // When Upload Settings is clicked, automatically pass the settings.json file to the MqttSendFile function

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsLoader.LoadSettings(); // Load the settings

            string ipAddress = GlobalSettings.ServerIP; // Get the IP Address from Settings

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


        // Opens file explorer window, then passes selected file to UploadFileToGoogleDrive, which does as it says on the tin.
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


        // Function used to upload file line by line via MQTT, generic function, passes commandpayload where it is handled by server. You should know the drill by now.

        private void genericUploadFunction(string commandPayload)
        {
            SettingsLoader.LoadSettings(); // Load the settings

            string ipAddress = GlobalSettings.ServerIP; // Get the IP address from settings

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string selectedDate = popups.ShowSingleDatePickerDialog();
                if (string.IsNullOrEmpty(selectedDate))
                {
                    MessageBox.Show("No date selected. Operation canceled.");
                    return;
                }

                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "All files (*.*)|*.*";
                    openFileDialog.Title = "Select a file";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = openFileDialog.FileName;
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


        // Centers Graph when button is pressed.

        private void centerButton_Click(object sender, EventArgs e)
        {
            Main_Plot.Plot.Axes.AutoScale();
            Main_Plot.Refresh();
        }


        // Removes API overlay Axis

        private void clearAPIButton_Click(object sender, EventArgs e)
        {
            parse_graph.ClearAPIOverlay();
        }

        private void ouraRingUpload_Click(object sender, EventArgs e)
        {
            genericUploadFunction("oura_ring_upload");
        }

        private void clearAllButton_Click(object sender, EventArgs e)
        {
            parse_graph.ClearAllData();
        }




        private async void stopListeningLiveButton_Click(object sender, EventArgs e)
        {
            await mqtt.ToggleLiveSubscription(stopListeningLiveButton);
        }

        private void samsungHRUpload_Click(object sender, EventArgs e)
        {
            genericUploadFunction("samsung_hr_upload");
        }

        private void samsungHRAPI_Click(object sender, EventArgs e)
        {
            getHealthAPIGeneral("Heart Rate", "fetch_samsung_hr,2");
        }


    }
}



