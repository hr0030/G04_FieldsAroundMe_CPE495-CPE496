using ScottPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using ScottPlot.WinForms;

public class Parse_Graph_Functions 
{

    // Declare voltagesByChannel as a dictionary to store voltage data for each channel
    public Dictionary<int, List<double>> voltagesByChannel = new Dictionary<int, List<double>>();
    public Dictionary<int, List<DateTime>> _Dates = new Dictionary<int, List<DateTime>>();
    // At the class level
    public List<DateTime> API_Dates = new List<DateTime>();
    public List<double> API_Magnitude = new List<double>();

    private FormsPlot Main_Plot;
    private FormsPlot API_Plot;

    public Parse_Graph_Functions(FormsPlot mainPlot, FormsPlot apiPlot)
    {
        this.Main_Plot = mainPlot;
        this.API_Plot = apiPlot;
    }

    public void LoadDataFromCsv(string filePath)
    {
        // Dictionary to hold separate lists for each channel (1-4)
        var voltagesByChannel = new Dictionary<int, List<double>>
{
    { 1, new List<double>() },
    { 2, new List<double>() },
    { 3, new List<double>() },
    { 4, new List<double>() }
};

        try
        {
            using (var reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // Skip header line

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
                        // Ensure the channel exists in _Dates
                        if (!_Dates.ContainsKey(channel))
                        {
                            _Dates[channel] = new List<DateTime>();
                        }

                        _Dates[channel].Add(dateTime);
                        voltagesByChannel[channel].Add(millivolts);
                    }
                }
            }

            // Call the plotting function for each channel
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
        // Parse the payload (expected format: "timestamp,channel,data")
        var parts = payload.Split(',');
        double voltage = 0; //Initialization to Avoid Errors
        int channel = 0;

        if (parts.Length == 3 &&
            DateTime.TryParse(parts[0], out DateTime timestamp) &&
            int.TryParse(parts[1], out channel) &&
            double.TryParse(parts[2], out voltage)) // Ensure channel is within range
        {
            // Ensure the channel exists in the dictionaries
            if (!voltagesByChannel.ContainsKey(channel))
            {
                voltagesByChannel[channel] = new List<double>();
            }
            if (!_Dates.ContainsKey(channel))
            {
                _Dates[channel] = new List<DateTime>();
            }
            // Add timestamp to the respective channel without checking for duplicates
            _Dates[channel].Add(timestamp);

            // Add voltage data to the corresponding channel list
            voltagesByChannel[channel].Add(voltage);

            // Invoke PlotData on the main thread
            Main_Plot.Invoke((MethodInvoker)(() =>
            {

                PlotData(voltagesByChannel, _Dates);
            }));
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

        // Ensure there are exactly 2 parts (timestamp and a single numerical value)
        if (parts.Length != 2)
        {
            Debug.WriteLine($"Invalid payload format: {payload}");
            return;
        }

        // Extract the timestamp from the payload
        string timestampStr = parts[0].Trim();
        if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
        {
            Debug.WriteLine($"Failed to parse timestamp: {timestampStr}");
            return;
        }

        // Extract the single numerical value
        string numericStr = parts[1].Trim();
        if (!double.TryParse(numericStr, out double numericValue))
        {
            Debug.WriteLine($"Failed to parse numerical value: {numericStr}");
            return;
        }

        // Add timestamp and numerical value to the global lists
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
        Debug.WriteLine("1");
        try
        {
            Main_Plot.Plot.Clear(); // Clear previous plots

            IPalette palette = new ScottPlot.Palettes.Category10();

            int colorIndex = 0;
            foreach (var channel in voltagesByChannel.Keys)
            {
                if (voltagesByChannel[channel].Count == 0 || !timestamps.ContainsKey(channel) || timestamps[channel].Count == 0)
                    continue; // Skip empty channels or channels without timestamps

                double[] xs = timestamps[channel].Select(date => date.ToOADate()).ToArray(); // Get timestamps for the current channel
                double[] ys = voltagesByChannel[channel].ToArray(); // Get voltages for the current channel

                var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

                linePlot.Label = $"Channel {channel}";
                linePlot.LineWidth = 2;
                linePlot.MarkerSize = 1;
                linePlot.Color = palette.GetColor(colorIndex++); // Get a unique color
                Debug.WriteLine(voltagesByChannel[channel]);
            }

            Main_Plot.Plot.Legend.IsVisible = true; // Show legend to differentiate channels
            Main_Plot.Plot.Axes.AutoScale(); // Auto-scale for better visibility
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

            // Convert DateTime to OADate for plotting
            double[] xs = timestamps.ConvertAll(date => date.ToOADate()).ToArray();
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

            // Auto-scale the plot and refresh
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

            // Convert DateTime to OADate for plotting
            double[] xs = timestamps.ConvertAll(date => date.ToOADate()).ToArray();
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

            // Auto-scale the plot and refresh
            API_Plot.Plot.Axes.AutoScale();
            API_Plot.Refresh();

            Debug.WriteLine("Lollipop graph refreshed.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting lollipop data: {ex.Message}");
        }
    }



}