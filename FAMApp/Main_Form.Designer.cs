using System;
using System.IO;
using System.Globalization; // Added for Parsing of CSV file
using System.Drawing;
using System.Windows.Forms;

namespace FAMApp
{
    partial class Main_Form
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
       

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main_Form));
            toolStrip1 = new ToolStrip();
            sourceButton1 = new ToolStripDropDownButton();
            wifiToolStripMenuItem = new ToolStripMenuItem();
            cloudToolStripMenuItem = new ToolStripMenuItem();
            microSDToolStripMenuItem = new ToolStripMenuItem();
            geomagnetAPIToolStripMenuItem = new ToolStripMenuItem();
            geomagneticStormsToolStripMenuItem = new ToolStripMenuItem();
            solarFlareToolStripMenuItem = new ToolStripMenuItem();
            temperatureToolStripMenuItem = new ToolStripMenuItem();
            humidityToolStripMenuItem = new ToolStripMenuItem();
            treeRhythmsToolStripMenuItem = new ToolStripMenuItem();
            solarIndexToolStripMenuItem = new ToolStripMenuItem();
            pressureToolStripMenuItem = new ToolStripMenuItem();
            moonPhaseToolStripMenuItem = new ToolStripMenuItem();
            sunsetSunriseToolStripMenuItem = new ToolStripMenuItem();
            ouraRingToolStripMenuItem = new ToolStripMenuItem();
            hRReserveToolStripMenuItem = new ToolStripMenuItem();
            rRToolStripMenuItem = new ToolStripMenuItem();
            hRVToolStripMenuItem = new ToolStripMenuItem();
            Settings_Button = new ToolStripButton();
            toolStripDropDownUpload = new ToolStripDropDownButton();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            treeRhythmsToolStripMenuItem1 = new ToolStripMenuItem();
            uAHSWIRLLToolStripMenuItem = new ToolStripMenuItem();
            cSVToCloudToolStripMenuItem = new ToolStripMenuItem();
            ouraRingToolStripMenuItem1 = new ToolStripMenuItem();
            clearAPIButton = new ToolStripButton();
            clearAllButton = new ToolStripButton();
            Center = new ToolStripButton();
            sensorSelectionComboBox = new ToolStripComboBox();
            stopListeningLiveButton = new ToolStripButton();
            samsungHRUpload = new ToolStripMenuItem();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(20, 20);
            toolStrip1.Items.AddRange(new ToolStripItem[] { sourceButton1, Settings_Button, toolStripDropDownUpload, clearAPIButton, clearAllButton, Center, sensorSelectionComboBox, stopListeningLiveButton });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(800, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // sourceButton1
            // 
            sourceButton1.DisplayStyle = ToolStripItemDisplayStyle.Text;
            sourceButton1.DropDownItems.AddRange(new ToolStripItem[] { wifiToolStripMenuItem, cloudToolStripMenuItem, microSDToolStripMenuItem, geomagnetAPIToolStripMenuItem });
            sourceButton1.Image = (Image)resources.GetObject("sourceButton1.Image");
            sourceButton1.ImageTransparentColor = Color.Magenta;
            sourceButton1.Name = "sourceButton1";
            sourceButton1.Size = new Size(56, 22);
            sourceButton1.Text = "Source";
            // 
            // wifiToolStripMenuItem
            // 
            wifiToolStripMenuItem.Name = "wifiToolStripMenuItem";
            wifiToolStripMenuItem.Size = new Size(180, 22);
            wifiToolStripMenuItem.Text = "Wifi";
            wifiToolStripMenuItem.Click += wifiToolStripMenuItem_Click;
            // 
            // cloudToolStripMenuItem
            // 
            cloudToolStripMenuItem.Name = "cloudToolStripMenuItem";
            cloudToolStripMenuItem.Size = new Size(180, 22);
            cloudToolStripMenuItem.Text = "Load from Cloud";
            cloudToolStripMenuItem.Click += cloudToolStripMenuItem_Click;
            // 
            // microSDToolStripMenuItem
            // 
            microSDToolStripMenuItem.Name = "microSDToolStripMenuItem";
            microSDToolStripMenuItem.Size = new Size(180, 22);
            microSDToolStripMenuItem.Text = "Load from Micro SD";
            microSDToolStripMenuItem.Click += microSDToolStripMenuItem_Click;
            // 
            // geomagnetAPIToolStripMenuItem
            // 
            geomagnetAPIToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { geomagneticStormsToolStripMenuItem, solarFlareToolStripMenuItem, temperatureToolStripMenuItem, humidityToolStripMenuItem, treeRhythmsToolStripMenuItem, solarIndexToolStripMenuItem, pressureToolStripMenuItem, moonPhaseToolStripMenuItem, sunsetSunriseToolStripMenuItem, ouraRingToolStripMenuItem });
            geomagnetAPIToolStripMenuItem.Name = "geomagnetAPIToolStripMenuItem";
            geomagnetAPIToolStripMenuItem.Size = new Size(180, 22);
            geomagnetAPIToolStripMenuItem.Text = "APIs";
            // 
            // geomagneticStormsToolStripMenuItem
            // 
            geomagneticStormsToolStripMenuItem.Name = "geomagneticStormsToolStripMenuItem";
            geomagneticStormsToolStripMenuItem.Size = new Size(185, 22);
            geomagneticStormsToolStripMenuItem.Text = "Geomagnetic Storms";
            geomagneticStormsToolStripMenuItem.Click += geomagneticStormsToolStripMenuItem_Click;
            // 
            // solarFlareToolStripMenuItem
            // 
            solarFlareToolStripMenuItem.Name = "solarFlareToolStripMenuItem";
            solarFlareToolStripMenuItem.Size = new Size(185, 22);
            solarFlareToolStripMenuItem.Text = "Solar Flare";
            solarFlareToolStripMenuItem.Click += solarFlareAPI_Click;
            // 
            // temperatureToolStripMenuItem
            // 
            temperatureToolStripMenuItem.Name = "temperatureToolStripMenuItem";
            temperatureToolStripMenuItem.Size = new Size(185, 22);
            temperatureToolStripMenuItem.Text = "Temperature";
            temperatureToolStripMenuItem.Click += Temperature_API_Click;
            // 
            // humidityToolStripMenuItem
            // 
            humidityToolStripMenuItem.Name = "humidityToolStripMenuItem";
            humidityToolStripMenuItem.Size = new Size(185, 22);
            humidityToolStripMenuItem.Text = "Humidity";
            humidityToolStripMenuItem.Click += Humidity_API_Click;
            // 
            // treeRhythmsToolStripMenuItem
            // 
            treeRhythmsToolStripMenuItem.Name = "treeRhythmsToolStripMenuItem";
            treeRhythmsToolStripMenuItem.Size = new Size(185, 22);
            treeRhythmsToolStripMenuItem.Text = "Tree Rhythms";
            treeRhythmsToolStripMenuItem.Click += Tree_Rhythms_API_Click;
            // 
            // solarIndexToolStripMenuItem
            // 
            solarIndexToolStripMenuItem.Name = "solarIndexToolStripMenuItem";
            solarIndexToolStripMenuItem.Size = new Size(185, 22);
            solarIndexToolStripMenuItem.Text = "Solar Index";
            solarIndexToolStripMenuItem.Click += Solar_Index_Click;
            // 
            // pressureToolStripMenuItem
            // 
            pressureToolStripMenuItem.Name = "pressureToolStripMenuItem";
            pressureToolStripMenuItem.Size = new Size(185, 22);
            pressureToolStripMenuItem.Text = "Pressure";
            pressureToolStripMenuItem.Click += Pressure_API_Click;
            // 
            // moonPhaseToolStripMenuItem
            // 
            moonPhaseToolStripMenuItem.Name = "moonPhaseToolStripMenuItem";
            moonPhaseToolStripMenuItem.Size = new Size(185, 22);
            moonPhaseToolStripMenuItem.Text = "Moon Phase";
            moonPhaseToolStripMenuItem.Click += Moon_Phase_Click;
            // 
            // sunsetSunriseToolStripMenuItem
            // 
            sunsetSunriseToolStripMenuItem.Name = "sunsetSunriseToolStripMenuItem";
            sunsetSunriseToolStripMenuItem.Size = new Size(185, 22);
            sunsetSunriseToolStripMenuItem.Text = "Sunset/Sunrise";
            sunsetSunriseToolStripMenuItem.Click += Sun_Rise_Set_API_Click;
            // 
            // ouraRingToolStripMenuItem
            // 
            ouraRingToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { hRReserveToolStripMenuItem, rRToolStripMenuItem, hRVToolStripMenuItem });
            ouraRingToolStripMenuItem.Name = "ouraRingToolStripMenuItem";
            ouraRingToolStripMenuItem.Size = new Size(185, 22);
            ouraRingToolStripMenuItem.Text = "Oura Ring";
            // 
            // hRReserveToolStripMenuItem
            // 
            hRReserveToolStripMenuItem.Name = "hRReserveToolStripMenuItem";
            hRReserveToolStripMenuItem.Size = new Size(133, 22);
            hRReserveToolStripMenuItem.Text = "HR Reserve";
            hRReserveToolStripMenuItem.Click += oura_Ring_HR_Reserve_Click;
            // 
            // rRToolStripMenuItem
            // 
            rRToolStripMenuItem.Name = "rRToolStripMenuItem";
            rRToolStripMenuItem.Size = new Size(133, 22);
            rRToolStripMenuItem.Text = "RR";
            rRToolStripMenuItem.Click += oura_Ring_RR_Click;
            // 
            // hRVToolStripMenuItem
            // 
            hRVToolStripMenuItem.Name = "hRVToolStripMenuItem";
            hRVToolStripMenuItem.Size = new Size(133, 22);
            hRVToolStripMenuItem.Text = "HRV";
            hRVToolStripMenuItem.Click += oura_Ring_Click;
            // 
            // Settings_Button
            // 
            Settings_Button.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Settings_Button.Image = (Image)resources.GetObject("Settings_Button.Image");
            Settings_Button.ImageTransparentColor = Color.Magenta;
            Settings_Button.Name = "Settings_Button";
            Settings_Button.Size = new Size(53, 22);
            Settings_Button.Text = "Settings";
            Settings_Button.Click += Settings_Button_Click;
            // 
            // toolStripDropDownUpload
            // 
            toolStripDropDownUpload.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripDropDownUpload.DropDownItems.AddRange(new ToolStripItem[] { settingsToolStripMenuItem, treeRhythmsToolStripMenuItem1, uAHSWIRLLToolStripMenuItem, cSVToCloudToolStripMenuItem, ouraRingToolStripMenuItem1, samsungHRUpload });
            toolStripDropDownUpload.Image = (Image)resources.GetObject("toolStripDropDownUpload.Image");
            toolStripDropDownUpload.ImageTransparentColor = Color.Magenta;
            toolStripDropDownUpload.Name = "toolStripDropDownUpload";
            toolStripDropDownUpload.Size = new Size(58, 22);
            toolStripDropDownUpload.Text = "Upload";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(180, 22);
            settingsToolStripMenuItem.Text = "Settings";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // treeRhythmsToolStripMenuItem1
            // 
            treeRhythmsToolStripMenuItem1.Name = "treeRhythmsToolStripMenuItem1";
            treeRhythmsToolStripMenuItem1.Size = new Size(180, 22);
            treeRhythmsToolStripMenuItem1.Text = "Tree Rhythms";
            treeRhythmsToolStripMenuItem1.Click += treeRhythmsUpload_Click;
            // 
            // uAHSWIRLLToolStripMenuItem
            // 
            uAHSWIRLLToolStripMenuItem.Name = "uAHSWIRLLToolStripMenuItem";
            uAHSWIRLLToolStripMenuItem.Size = new Size(180, 22);
            uAHSWIRLLToolStripMenuItem.Text = "UAH SWIRLL";
            uAHSWIRLLToolStripMenuItem.Click += UAH_SWIRLL_Upload_Click;
            // 
            // cSVToCloudToolStripMenuItem
            // 
            cSVToCloudToolStripMenuItem.Name = "cSVToCloudToolStripMenuItem";
            cSVToCloudToolStripMenuItem.Size = new Size(180, 22);
            cSVToCloudToolStripMenuItem.Text = "CSV to Cloud";
            cSVToCloudToolStripMenuItem.Click += Upload_CSV_Cloud_Click;
            // 
            // ouraRingToolStripMenuItem1
            // 
            ouraRingToolStripMenuItem1.Name = "ouraRingToolStripMenuItem1";
            ouraRingToolStripMenuItem1.Size = new Size(180, 22);
            ouraRingToolStripMenuItem1.Text = "Oura Ring";
            ouraRingToolStripMenuItem1.Click += ouraRingUpload_Click;
            // 
            // clearAPIButton
            // 
            clearAPIButton.BackgroundImageLayout = ImageLayout.None;
            clearAPIButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            clearAPIButton.Image = (Image)resources.GetObject("clearAPIButton.Image");
            clearAPIButton.ImageTransparentColor = Color.Magenta;
            clearAPIButton.Name = "clearAPIButton";
            clearAPIButton.Size = new Size(59, 22);
            clearAPIButton.Text = "Clear API";
            clearAPIButton.Click += clearAPIButton_Click;
            // 
            // clearAllButton
            // 
            clearAllButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            clearAllButton.Image = (Image)resources.GetObject("clearAllButton.Image");
            clearAllButton.ImageTransparentColor = Color.Magenta;
            clearAllButton.Name = "clearAllButton";
            clearAllButton.Size = new Size(55, 22);
            clearAllButton.Text = "Clear All";
            clearAllButton.ToolTipText = "Clear All Data on Current Graph";
            clearAllButton.Click += clearAllButton_Click;
            // 
            // Center
            // 
            Center.DisplayStyle = ToolStripItemDisplayStyle.Text;
            Center.Image = (Image)resources.GetObject("Center.Image");
            Center.ImageTransparentColor = Color.Magenta;
            Center.Name = "Center";
            Center.Size = new Size(46, 22);
            Center.Text = "Center";
            Center.Click += centerButton_Click;
            // 
            // sensorSelectionComboBox
            // 
            sensorSelectionComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sensorSelectionComboBox.Items.AddRange(new object[] { "S1", "S2", "S3" });
            sensorSelectionComboBox.Name = "sensorSelectionComboBox";
            sensorSelectionComboBox.Size = new Size(121, 25);
            sensorSelectionComboBox.ToolTipText = "Select your Desired Sensor";
            // 
            // stopListeningLiveButton
            // 
            stopListeningLiveButton.BackColor = Color.LightCoral;
            stopListeningLiveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            stopListeningLiveButton.Image = (Image)resources.GetObject("stopListeningLiveButton.Image");
            stopListeningLiveButton.ImageTransparentColor = Color.Magenta;
            stopListeningLiveButton.Name = "stopListeningLiveButton";
            stopListeningLiveButton.Size = new Size(59, 22);
            stopListeningLiveButton.Text = "Stop Live";
            stopListeningLiveButton.ToolTipText = "Stop Adding Additional Data Points to Live Graph";
            stopListeningLiveButton.Click += stopListeningLiveButton_Click;
            // 
            // samsungHRUpload
            // 
            samsungHRUpload.Name = "samsungHRUpload";
            samsungHRUpload.Size = new Size(180, 22);
            samsungHRUpload.Text = "Samsung HR";
            samsungHRUpload.Click += samsungHRUpload_Click;
            // 
            // Main_Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(toolStrip1);
            Name = "Main_Form";
            Text = "Fields Around Me";
            Load += Main_Form_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion

        private ToolStrip toolStrip1;
        private ToolStripDropDownButton sourceButton1;
        private ToolStripMenuItem wifiToolStripMenuItem;
        private ToolStripMenuItem cloudToolStripMenuItem;
        private ToolStripMenuItem microSDToolStripMenuItem;







        private string[] GenerateTimeLabels(double samplingFrequency, int numberOfPoints)
        {
                    // Temporary, must implement real time stamps
            string[] timeLabels = new string[numberOfPoints];
            double timeInterval = 1 / samplingFrequency; 
            for (int i = 0; i < numberOfPoints; i++)
            {
                timeLabels[i] = (i * timeInterval).ToString("F2");
            }
            return timeLabels;
        }

        private ToolStripMenuItem geomagnetAPIToolStripMenuItem;
        private ToolStripMenuItem geomagneticStormsToolStripMenuItem;
        private ToolStripMenuItem temperatureToolStripMenuItem;
        private ToolStripMenuItem humidityToolStripMenuItem;
        private ToolStripButton Settings_Button;
        private ToolStripMenuItem treeRhythmsToolStripMenuItem;
        private ToolStripDropDownButton toolStripDropDownUpload;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem treeRhythmsToolStripMenuItem1;
        private ToolStripMenuItem solarIndexToolStripMenuItem;
        private ToolStripMenuItem pressureToolStripMenuItem;
        private ToolStripMenuItem uAHSWIRLLToolStripMenuItem;
        private ToolStripMenuItem cSVToCloudToolStripMenuItem;
        private ToolStripButton clearAPIButton;
        private ToolStripButton Center;
        private ToolStripMenuItem moonPhaseToolStripMenuItem;
        private ToolStripMenuItem solarFlareToolStripMenuItem;
        private ToolStripMenuItem sunsetSunriseToolStripMenuItem;
        private ToolStripMenuItem ouraRingToolStripMenuItem;
        private ToolStripMenuItem hRReserveToolStripMenuItem;
        private ToolStripMenuItem rRToolStripMenuItem;
        private ToolStripMenuItem hRVToolStripMenuItem;
        private ToolStripMenuItem ouraRingToolStripMenuItem1;
        private ToolStripComboBox sensorSelectionComboBox;
        private ToolStripButton clearAllButton;
        private ToolStripButton stopListeningLiveButton;
        private ToolStripMenuItem samsungHRUpload;
    }
}
