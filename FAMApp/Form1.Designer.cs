using System;
using System.IO;
using System.Globalization; // Added for Parsing of CSV file
using LiveChartsCore; // Core library
using LiveChartsCore.SkiaSharpView; // Core components
using LiveChartsCore.SkiaSharpView.WinForms; // For WinForms components
using System.Drawing;
using System.Windows.Forms;
using LiveCharts.Wpf;
using LiveCharts;

namespace FAMApp
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            toolStrip1 = new ToolStrip();
            sourceButton1 = new ToolStripDropDownButton();
            wifiToolStripMenuItem = new ToolStripMenuItem();
            cloudToolStripMenuItem = new ToolStripMenuItem();
            microSDToolStripMenuItem = new ToolStripMenuItem();
            cartesianChart1 = new LiveChartsCore.SkiaSharpView.WinForms.CartesianChart();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { sourceButton1 });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(800, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // sourceButton1
            // 
            sourceButton1.DisplayStyle = ToolStripItemDisplayStyle.Text;
            sourceButton1.DropDownItems.AddRange(new ToolStripItem[] { wifiToolStripMenuItem, cloudToolStripMenuItem, microSDToolStripMenuItem });
            sourceButton1.Image = (Image)resources.GetObject("sourceButton1.Image");
            sourceButton1.ImageTransparentColor = Color.Magenta;
            sourceButton1.Name = "sourceButton1";
            sourceButton1.Size = new Size(56, 22);
            sourceButton1.Text = "Source";
            sourceButton1.Click += sourceButton1_Click;
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
            // cartesianChart1
            // 
            cartesianChart1.AutoSize = true;
            cartesianChart1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cartesianChart1.Dock = DockStyle.Fill;
            cartesianChart1.Location = new Point(0, 25);
            cartesianChart1.Name = "cartesianChart1";
            cartesianChart1.Size = new Size(800, 425);
            cartesianChart1.TabIndex = 1;
            cartesianChart1.Load += cartesianChart1_Load;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(cartesianChart1);
            Controls.Add(toolStrip1);
            Name = "Form1";
            Text = "Fields Around Me";
            Load += Form1_Load;
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
        private LiveChartsCore.SkiaSharpView.WinForms.CartesianChart cartesianChart1;




        private void LoadCSVAndPlot(string filePath)
        {
            try
            {
                // Check if .csv
                if (Path.GetExtension(filePath).ToLower() != ".csv")
                {
                    MessageBox.Show("Select a CSV File", "Invalid File Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string[] csvLines = File.ReadAllLines(filePath);

                //Check file has at least 3 lines (frequency, timestamp, data)
                if (csvLines.Length < 3)
                {
                    MessageBox.Show("The CSV file is missing necessary data.", "Invalid CSV Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get Frequency and Timestamp
                double samplingFrequency = double.Parse(csvLines[0], CultureInfo.InvariantCulture);
                DateTime timestamp = DateTime.Parse(csvLines[1], CultureInfo.InvariantCulture);

                // Get data from third line
                string[] dataPoints = csvLines[2].Split(',');
                var values = Array.ConvertAll(dataPoints, s => double.Parse(s, CultureInfo.InvariantCulture));

                var series = new LineSeries<double>
                {
                    Values = values
                };

                cartesianChart1.Series = new ISeries[] { series };

                // Configure axes
                cartesianChart1.XAxes = new LiveChartsCore.SkiaSharpView.Axis[]
                {
            new LiveChartsCore.SkiaSharpView.Axis
            {
                Name = "Time (s)",
                Labels = GenerateTimeLabels(samplingFrequency, values.Length)
            }
                };

                cartesianChart1.YAxes = new LiveChartsCore.SkiaSharpView.Axis[]
                {
            new LiveChartsCore.SkiaSharpView.Axis
            {
                Name = "Values"
            }
                };
            }
            catch (FormatException fe)
            {
                MessageBox.Show($"File format error: {fe.Message}. Ensure the CSV has data in correct format(Frequency, Timestamp, Data)", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading and plotting CSV data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


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
    }
}
