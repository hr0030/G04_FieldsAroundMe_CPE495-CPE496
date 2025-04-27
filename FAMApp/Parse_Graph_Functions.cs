using ScottPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using ScottPlot.WinForms;
using FAMApp;
using static Moon_Phase_Calculator;
public class Parse_Graph_Functions 
{

    public Dictionary<int, List<double>> voltagesByChannel = new Dictionary<int, List<double>>();
    public Dictionary<int, List<DateTime>> _Dates = new Dictionary<int, List<DateTime>>();
    public List<DateTime> API_Dates = new List<DateTime>();
    public List<double> API_Magnitude = new List<double>();

    private FormsPlot Main_Plot;
    private FormsPlot API_Plot;

    private System.Timers.Timer plotTimer;
    private bool isTimerStarted = false;
    private bool newDataAvailable = false;


    private void StartPlotTimer()
    {
        plotTimer = new System.Timers.Timer(2000); // interval in milliseconds
        plotTimer.Elapsed += PlotTimer_Tick;
        plotTimer.AutoReset = true;
        plotTimer.Enabled = true;
        isTimerStarted = true;
        Debug.WriteLine("Timer has been started");
    }

    private void PlotTimer_Tick(object sender, EventArgs e)
    {
        Debug.WriteLine("Timer has triggered");
        if (newDataAvailable)
        {
            Main_Plot.Invoke((MethodInvoker)(() =>
            {
                Debug.WriteLine("PlotData Invoked");
                PlotData(voltagesByChannel, _Dates);
            }));

            newDataAvailable = false;
        }
    }




    public Parse_Graph_Functions(FormsPlot mainPlot, FormsPlot apiPlot)
    {
        this.Main_Plot = mainPlot;
        this.API_Plot = apiPlot;
    }

    public void LoadDataFromCsv(string filePath)
    {
        /* var voltagesByChannel = new Dictionary<int, List<double>>
{
    { 1, new List<double>() },
    { 2, new List<double>() },
    { 3, new List<double>() },
    { 4, new List<double>() }
}; */

        try
        {
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // Skip header

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var columns = line.Split(',');

                    if (columns.Length >= 3 &&
                        DateTime.TryParse(columns[0], out DateTime dateTime) &&
                        int.TryParse(columns[1], out int channel) &&
                        double.TryParse(columns[2], out double millivolts) &&
                        channel >= 1 && channel <= 4) // Ensure valid channel range
                    {

                        if (!_Dates.ContainsKey(channel))
                        {
                            _Dates[channel] = new List<DateTime>();
                        }

                        if (!voltagesByChannel.ContainsKey(channel))
                        {
                            voltagesByChannel[channel] = new List<double>();
                        }
                        _Dates[channel].Add(dateTime);
                        voltagesByChannel[channel].Add(millivolts);
                    }
                }
            }


            Main_Plot.Invoke((MethodInvoker)(() =>
            {
                PlotData(voltagesByChannel, _Dates);
            }));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading CSV data: {ex.Message}");
        }
    }



    public void ParseAndGraphLiveData(string payload)
    {
        var parts = payload.Split(',');
        double voltage = 0;
        int channel = 0;

        if (parts.Length == 3 &&
            DateTime.TryParse(parts[0], out DateTime timestamp) &&
            int.TryParse(parts[1], out channel) &&
            double.TryParse(parts[2], out voltage))
        {
            if (!voltagesByChannel.ContainsKey(channel))
            {
                voltagesByChannel[channel] = new List<double>();
            }
            if (!_Dates.ContainsKey(channel))
            {
                _Dates[channel] = new List<DateTime>();
            }

            _Dates[channel].Add(timestamp);
            voltagesByChannel[channel].Add(voltage);

            newDataAvailable = true;  // <=== TELL the timer we have new data

            // Start timer if not already started
            if (!isTimerStarted)
            {
                StartPlotTimer();
            }
        }
    }



    public void ParseAPI(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            Debug.WriteLine("Payload is null or empty.");
            return;
        }

        var parts = payload.Split(',');

        
        if (parts.Length != 2)
        {
            Debug.WriteLine($"Invalid payload format: {payload}");
            return;
        }

        string timestampStr = parts[0].Trim(); // Get Timestamp
        if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
        {
            Debug.WriteLine($"Failed to parse timestamp: {timestampStr}");
            return;
        }

        string numericStr = parts[1].Trim(); // Get Data
        if (!double.TryParse(numericStr, out double numericValue))
        {
            Debug.WriteLine($"Failed to parse numerical value: {numericStr}");
            return;
        }

        API_Dates.Add(timestamp);
        API_Magnitude.Add(numericValue);
    }

    public void GraphAPI(string graphType)
    {
        if (API_Plot != null)
        {
            if (graphType == "line")
            {
                if (API_Plot.InvokeRequired)
                {
                    API_Plot.Invoke((MethodInvoker)(() =>
                    {
                        PlotLineAPIData(API_Dates, API_Magnitude);
                    }));
                }
                else
                {
                    PlotLineAPIData(API_Dates, API_Magnitude);
                }
            }
            else if (graphType == "lollipop")
                if (API_Plot.InvokeRequired)
                {
                    API_Plot.Invoke((MethodInvoker)(() =>
                    {
                        PlotLollipopData(API_Dates, API_Magnitude);
                    }));
                }
                else
                {
                    PlotLollipopData(API_Dates, API_Magnitude);
                }

        }
        else
        {
            Debug.WriteLine("newAPIPlot is null. Unable to update plot.");
        }
    }

    public void PlotData(Dictionary<int, List<double>> voltagesByChannel, Dictionary<int, List<DateTime>> timestamps)
    {
        try
        {
            Main_Plot.Plot.Clear(); // Clear plot

            IPalette palette = new ScottPlot.Palettes.Category10();

            int colorIndex = 0;
            foreach (var channel in voltagesByChannel.Keys)
            {
                if (voltagesByChannel[channel].Count == 0 || !timestamps.ContainsKey(channel) || timestamps[channel].Count == 0)
                    continue; // Skip empty channels

                double[] xs = timestamps[channel].Select(date => date.ToOADate()).ToArray(); // Get timestamps for the current channel
                double[] ys = voltagesByChannel[channel].ToArray(); // Get voltages for the current channel

                var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

                linePlot.Label = $"Channel {channel}";
                linePlot.LineWidth = 2;
                linePlot.MarkerSize = 1;
                linePlot.Color = palette.GetColor(colorIndex++); // Get a unique color
                Debug.WriteLine(voltagesByChannel[channel]);
            }

            Main_Plot.Plot.Legend.IsVisible = true;
            Main_Plot.Plot.Axes.AutoScale();
            Main_Plot.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting data: {ex.Message}");
        }
    }


    public void PlotAPIDataOverlay()
    {
        try
        {

            IPalette palette = new ScottPlot.Palettes.Category10();

            int colorIndex = 4;


            double[] xs = API_Dates.ConvertAll(date => date.ToOADate()).ToArray();
            double[] ys = API_Magnitude.ToArray();

            var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

            linePlot.Axes.YAxis = Main_Plot.Plot.Axes.Right;
            linePlot.Label = "API";
            linePlot.LineWidth = 2;
            linePlot.MarkerSize = 1;
            linePlot.Color = palette.GetColor(colorIndex++); // Get a unique color
          
            Main_Plot.Plot.Legend.IsVisible = true; 
            Main_Plot.Plot.Axes.AutoScale();
            Main_Plot.Refresh();


        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting data: {ex.Message}");
        }
    }

    public void PlotLineAPIData(List<DateTime> timestamps, List<double> data)
    {
        try
        {
            if (timestamps == null || data == null || timestamps.Count == 0 || data.Count == 0)
            {
                MessageBox.Show("No data to plot. Please ensure timestamps and data are populated.");
                return;
            }

            
            double[] xs = timestamps.ConvertAll(date => date.ToOADate()).ToArray(); // Convert DateTime to OADate
            double[] ys = data.ToArray();

            if (xs.Length != ys.Length)
            {
                MessageBox.Show("Mismatch between timestamps and data lengths.");
                return;
            }

            Debug.WriteLine($"Plotting line graph with {xs.Length} points.");
            API_Plot.Plot.Clear();
            var linePlot = API_Plot.Plot.Add.Scatter(xs, ys);
            linePlot.LineWidth = 2;
            linePlot.MarkerSize = 1;

            API_Plot.Plot.Axes.AutoScale();
            API_Plot.Refresh();

            Debug.WriteLine("Line graph refreshed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting line data: {ex.Message}");
        }
    }
    public void PlotLollipopData(List<DateTime> timestamps, List<double> data)
    {
        try
        {
            if (timestamps == null || data == null || timestamps.Count == 0 || data.Count == 0)
            {
                MessageBox.Show("No data to plot. Please ensure timestamps and data are populated.");
                return;
            }

            double[] xs = timestamps.ConvertAll(date => date.ToOADate()).ToArray(); // Convert DateTime to OADate
            double[] ys = data.ToArray();

            if (xs.Length != ys.Length)
            {
                MessageBox.Show("Mismatch between timestamps and data lengths.");
                return;
            }

            Debug.WriteLine($"Plotting lollipop graph with {xs.Length} points.");
            API_Plot.Plot.Clear();

            // Add lollipop sticks
            for (int i = 0; i < xs.Length; i++)
            {
                var stick = API_Plot.Plot.Add.Scatter(
                    xs: new double[] { xs[i], xs[i] },
                    ys: new double[] { 0, ys[i] }
                );
                stick.LineWidth = 1; // Set line width
            }

            var scatterPlot = API_Plot.Plot.Add.Scatter(xs, ys);
            // scatterPlot.Label = label;
            scatterPlot.MarkerSize = 10;
            scatterPlot.LineStyle = ScottPlot.LineStyle.None;

            API_Plot.Plot.Axes.AutoScale();
            API_Plot.Refresh();

            Debug.WriteLine("Lollipop graph refreshed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting lollipop data: {ex.Message}");
        }
    }

    public void ClearAPIOverlay()
    {
        try
        {
            var apiPlot = Main_Plot.Plot.GetPlottables()
                            .OfType<ScottPlot.Plottables.Scatter>()
                            .FirstOrDefault(p => p.Label == "API");

            if (apiPlot != null)
            {
                Main_Plot.Plot.Remove(apiPlot);
                Main_Plot.Plot.Axes.AutoScale();
                Main_Plot.Refresh();
            }
            else
            {
                MessageBox.Show("API overlay not found.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error removing API overlay: {ex.Message}");
        }
    }

    public void ClearAllData()
    {
        try
        {
            Main_Plot.Plot.Clear(); 
            Main_Plot.Plot.Axes.AutoScale(); 
            Main_Plot.Refresh();

            voltagesByChannel.Clear(); // Clear voltage data
            _Dates.Clear(); // Clear timestamp data
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error clearing plot: {ex.Message}");
        }
    }



    public void GenerateMoonPhaseData(DateTime startDate, DateTime endDate)
    {
        API_Dates.Clear();
        API_Magnitude.Clear();

        for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            MoonPhaseData data = Moon_Phase_Calculator.Calculate(date);

            API_Dates.Add(date);
            API_Magnitude.Add(data.Illumination); // or data.Phase or data.Age, depending on graph

            // Optional: Log more details
            Console.WriteLine($"{date.ToShortDateString()} - {data.PhaseName} ({data.ZodiacName}) - Age: {data.Age:F2} days - Illumination: {data.Illumination:P0}");
        }

        PlotLollipopData(API_Dates, API_Magnitude);
    }




}