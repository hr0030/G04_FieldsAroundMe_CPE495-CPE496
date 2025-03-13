using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace FAMApp
{
    public partial class Settings_Form : Form
    {
        public Settings_Form()
        {
            InitializeComponent();
        }

        public class Sensor
        {
            public string Name { get; set; }
            public string Longitude { get; set; }
            public string Latitude { get; set; }
        }

        public class Settings
        {
            public string ServerIP { get; set; }
            public string SamplingFrequency { get; set; }
            public List<Sensor> Sensors { get; set; } = new List<Sensor>();
        }

        private void Settings_Form_Load(object sender, EventArgs e)
        {
            string filePath = "settings.json";
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Settings settings = JsonConvert.DeserializeObject<Settings>(json);

                IP_Address_Textbox.Text = settings.ServerIP;
                Sampling_Frequency_Textbox.Text = settings.SamplingFrequency;
            }
        }

        private void Save_Settings_Button_Click(object sender, EventArgs e)
        {
            try
            {
                string filePath = "settings.json";
                Settings settings;

                // Load existing settings if the file exists, otherwise create a new one
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
                else
                {
                    settings = new Settings();
                }

                // Always update IP and Sampling Frequency
                settings.ServerIP = IP_Address_Textbox.Text;
                settings.SamplingFrequency = Sampling_Frequency_Textbox.Text;

                int sensorNumber;
                if (int.TryParse(Sensor_Number_Textbox.Text, out sensorNumber) && sensorNumber >= 1 && sensorNumber <= 3)
                {
                    // Ensure the list has enough sensors
                    while (settings.Sensors.Count < sensorNumber)
                    {
                        settings.Sensors.Add(new Sensor { Name = "", Longitude = "", Latitude = "" });
                    }

                    settings.Sensors[sensorNumber - 1].Name = Sensor_Name_Textbox.Text;
                    settings.Sensors[sensorNumber - 1].Longitude = Longitude_Textbox.Text;
                    settings.Sensors[sensorNumber - 1].Latitude = Latitude_Textbox.Text;
                }

                // Save updated settings to file
                string jsonOutput = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, jsonOutput);

                MessageBox.Show("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }


    }
}