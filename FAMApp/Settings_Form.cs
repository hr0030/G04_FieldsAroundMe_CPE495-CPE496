using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public class Settings
        {
            public string ServerIP { get; set; }
        }

        private void Settings_Form_Load(object sender, EventArgs e)
        {
            string filePath = "settings.json";
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Settings settings = JsonConvert.DeserializeObject<Settings>(json);

                // Set the IP address to the TextBox
                IP_Address_Textbox.Text = settings.ServerIP;
            }
        }

        private void Save_Settings_Button_Click(object sender, EventArgs e)
    {
        try
        {
            // Get the IP address from the TextBox
            string ipAddress = IP_Address_Textbox.Text;
            Settings settings = new Settings
            {
                ServerIP = ipAddress
            };
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            string filePath = "settings.json";
            File.WriteAllText(filePath, json);
            MessageBox.Show("Settings saved successfully.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}");
        }
    }
    }
}
