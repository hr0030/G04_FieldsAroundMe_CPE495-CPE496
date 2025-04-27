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
                if(settings.SamplingFrequency == "100")
                {
                    Hundred_Hz_Button.Enabled = false;
                    Two_Hundred_Hz_Button.Enabled = true;
                }
                else
                {
                    Hundred_Hz_Button.Enabled = true;
                    Two_Hundred_Hz_Button.Enabled = false;
                }
            }
        }

        private void Save_Settings_Button_Click(object sender, EventArgs e)
        {
            try
            {
                string filePath = "settings.json";
                Settings settings;

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    settings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                }
                else
                {
                    settings = new Settings();
                }
                settings.ServerIP = IP_Address_Textbox.Text;
                if(Hundred_Hz_Button.Enabled == false)
                { 
                    settings.SamplingFrequency = "100"; 
                }
                else
                {
                    settings.SamplingFrequency = "250";
                }

                int sensorNumber;
                if (int.TryParse(Sensor_Number_Textbox.Text, out sensorNumber) && sensorNumber >= 1 && sensorNumber <= 3)
                {
                    while (settings.Sensors.Count < sensorNumber)
                    {
                        settings.Sensors.Add(new Sensor { Name = "", Longitude = "", Latitude = "" });
                    }

                    settings.Sensors[sensorNumber - 1].Name = Sensor_Name_Textbox.Text;
                    settings.Sensors[sensorNumber - 1].Longitude = Longitude_Textbox.Text;
                    settings.Sensors[sensorNumber - 1].Latitude = Latitude_Textbox.Text;
                }

                string jsonOutput = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, jsonOutput);

                MessageBox.Show("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void Hundred_Hz_Click(object sender, EventArgs e)
        {
            Hundred_Hz_Button.Enabled = false;
            Two_Hundred_Hz_Button.Enabled = true;
        }

        private void Two_Hundred_Hz_Button_Click(object sender, EventArgs e)
        {
            Hundred_Hz_Button.Enabled = true;
            Two_Hundred_Hz_Button.Enabled = false;
        }
    }
}