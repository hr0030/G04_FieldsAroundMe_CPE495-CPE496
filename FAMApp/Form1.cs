using LiveChartsCore.Defaults;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Immutable;
using static System.Runtime.InteropServices.JavaScript.JSType;
using LiveCharts.Wpf.Charts.Base;
using System.ComponentModel;
using ScottPlot.WinForms;

namespace FAMApp
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            InitializeChart();
            //ChartSettings();
        }
        private void sourceButton1_Click(object sender, EventArgs e)
        {

        }

        private void wifiToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void cloudToolStripMenuItem_Click(object sender, EventArgs e)
        {

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

        private void cartesianChart1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private DateTimePoint point = new DateTimePoint(DateTime.Now, 0.0);
        private DateTime dateTime = DateTime.Now;
        private LineSeries<DateTimePoint> lineSeries;

        private FormsPlot formsPlot;

//        private void InitializeChart()
//        {
//            long valuedt = dateTime.Ticks;
//            // Initialize the chart control
//
//            // Initialize the series (empty for now)
//            lineSeries = new LineSeries<DateTimePoint>
//            {
//                Name = "Voltage (mV)"
//            };
//
//            var labels = new List<string> { new DateTime(valuedt).ToString("yyyy-MM-dd HH:mm:ss"), new DateTime(valuedt+1).ToString("yyyy-MM-dd HH:mm:ss") };
//            var startDate = new DateTime(valuedt);
//            int i = 0;
//
//            // Set up the axes
//            cartesianChart1.XAxes = new List<Axis>
//            {
//                new Axis
//                {
//                    Labeler = value => 
//                    {
//                            DateTime labelDateTime = startDate.Add(TimeSpan.FromMinutes(1));
//                            return labelDateTime.ToString("yyyy-MM-dd HH:mm");
//                    },
//                    LabelsRotation = 25,
//                    Name = "Time"
//                }
//            };
//            cartesianChart1.YAxes = new List<Axis>
//            {
//                new Axis
//                {
//                    Name = "Voltage (mV)",
//                    Labeler = value => $"{value} mV"
//                }
//            };
//
//            // Add the series to the chart
//            cartesianChart1.Series = new ISeries[] { lineSeries };
//        }

        private void InitializeChart()
        {
            // Initialize the FormsPlot control
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


//        private void LoadCsvData(string filePath)
//        {
//
//            // Check if .csv
//            if (Path.GetExtension(filePath).ToLower() != ".csv")
//            {
//                MessageBox.Show("Select a CSV File", "Invalid File Type", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }
//
//            var values = new List<DateTimePoint>();
//            List<long> dates = new List<long>();
//            string DateValue;
//            int count = 0;
//
//            try
//            {
//                using (var csvReader = new StreamReader(filePath))
//                {
//                    // Skip header row
//                    csvReader.ReadLine();
//
//                    // Read data rows
//                    while (!csvReader.EndOfStream)
//                    {
//                        var line = csvReader.ReadLine();
//                        var columns = line.Split(',');
//
//                        if (double.TryParse(columns[1], out double millivolts)
//                            && DateTime.TryParse(columns[0], out DateTime dateTime))
//                        {
//                            dates.Add(dateTime.Ticks);
//                            values.Add(new DateTimePoint(dateTime, millivolts));
//                        }
//                        count = count + 1;
//                    }
//                }
//
//                var labels = new List<string> { new DateTime(dates[0]).ToString("yyyy-MM-dd HH:mm:ss"), new DateTime(dates[1]).ToString("yyyy-MM-dd HH:mm:ss") };
//
//                if (values.Count > 0)
//                {
//                    var minDateTime = values.Min(point => point.DateTime);
//                    var maxDateTime = values.Max(point => point.DateTime);
//                    var minVoltage = values.Min(point => point.Value);
//                    var maxVoltage = values.Max(point => point.Value);
//
//                    // Initialize the series (empty for now)
//                    lineSeries = new LineSeries<DateTimePoint>
//                    {
//                        Name = "Voltage (mV)",
//                        Values = values
//                    };
//
//                    int i = 0;
//
//                    // Set up the axes
//                    cartesianChart1.XAxes = new List<Axis>
//                    {
//                        new Axis
//                        {
//                            MinLimit = minDateTime.Ticks,
//                            MaxLimit = maxDateTime.Ticks,
//                                                Labeler = value =>
//                    {
//                        int index = (int)i;
//                        return index >= 0 && index < labels.Count ? labels[index] : "";
//                    },
//                            LabelsRotation = 25,
//                            Name = "Time"
//                        }
//                    };
//
//                    cartesianChart1.YAxes = new List<Axis>
//                    {
//                        new Axis
//                        {
//                            Name = "Voltage (mV)",
//                            Labeler = value => $"{value} mV"
//                        }
//                    };
//
//                    cartesianChart1.Series = new ISeries[] { lineSeries };
//                    cartesianChart1.Update();
//                }
//
//                cartesianChart1.Update(); // Refresh chart to display new data
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Error reading CSV data: {ex.Message}");
//            }
//        }

            private void LoadDataFromCsv(string filePath)
            {
                var dates = new List<DateTime>();
                var voltages = new List<double>();

                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        reader.ReadLine(); // Skip the header row

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

                    // Convert DateTime list to OADate for ScottPlot
                    double[] xs = dates.ConvertAll(date => date.ToOADate()).ToArray();
                    double[] ys = voltages.ToArray();

                    // Plot data on the chart
                    var linePlot = formsPlot.Plot.Add.Scatter(xs, ys);
                    linePlot.LegendText = "Voltage(mV)";
                    linePlot.LineWidth = 2;
                    linePlot.MarkerSize = 1;

                // Update and render the chart
                    formsPlot.Plot.Axes.AutoScale();
                    formsPlot.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading CSV data: {ex.Message}");
                }
            }

//            public void ChartSettings()
//            {
//                cartesianChart1.TooltipPosition = TooltipPosition.Hidden;
//                cartesianChart1.EasingFunction = null;
//            }
    }
}
