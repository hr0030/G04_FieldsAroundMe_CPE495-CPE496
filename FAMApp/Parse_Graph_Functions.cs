using ScottPlot;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;
using ScottPlot.WinForms;
using FAMApp;
using static Moon_Phase_Calculator;
using ScottPlot.Plottables;
public class Parse_Graph_Functions
{

    public Dictionary<int, List<double>> voltagesByChannel = new Dictionary<int, List<double>>();
    public Dictionary<int, List<DateTime>> _Dates = new Dictionary<int, List<DateTime>>();

    public List<DateTime> API_Dates = new List<DateTime>();
    public List<double> API_Magnitude = new List<double>();

    public List<DateTime> API_Correlation_1_Dates = new List<DateTime>();
    public List<double> API_Correlation_1_Magnitude = new List<double>();

    public List<DateTime> API_Correlation_2_Dates = new List<DateTime>();
    public List<double> API_Correlation_2_Magnitude = new List<double>();

    public List<DateTime> Correlation_Dates = new List<DateTime>();
    public List<double> Correlation_Magnitude = new List<double>();

    private FormsPlot Main_Plot;
    private FormsPlot API_Plot;

    private System.Timers.Timer plotTimer;
    private bool isTimerStarted = false;
    private bool newDataAvailable = false;


    public void SortAndFilterVoltagesByTimestamp(bool applyVoltageFilter)
    {
        foreach (var channel in _Dates.Keys.ToList())
        {
            if (_Dates.ContainsKey(channel) && voltagesByChannel.ContainsKey(channel))
            {
                var combinedList = _Dates[channel]
                    .Zip(voltagesByChannel[channel], (date, voltage) => new { Date = date, Voltage = voltage })
                    .OrderBy(entry => entry.Date) // Sort by timestamp
                    .ToList();

                List<DateTime> filteredDates = new List<DateTime>();
                List<double> filteredVoltages = new List<double>();

                if (applyVoltageFilter)
                {
                    // Filter the data based on 20% tolerance
                    for (int i = 0; i < combinedList.Count; i++)
                    {
                        var current = combinedList[i];
                        if (i == 0 || Math.Abs(current.Voltage - filteredVoltages.Last()) <= Math.Abs(filteredVoltages.Last() * 0.2))
                        {
                            filteredDates.Add(current.Date);
                            filteredVoltages.Add(current.Voltage);
                        }
                    }
                }
                else
                {
                    filteredDates = combinedList.Select(entry => entry.Date).ToList();
                    filteredVoltages = combinedList.Select(entry => entry.Voltage).ToList();
                }

                
                _Dates[channel] = filteredDates;
                voltagesByChannel[channel] = filteredVoltages;
            }
        }
    }



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
                SortAndFilterVoltagesByTimestamp(true);
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
                SortAndFilterVoltagesByTimestamp(true);
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

            newDataAvailable = true; 

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

        var lines = payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            // Debug.WriteLine(line);
            if (parts.Length != 2)
            {
                Debug.WriteLine($"Invalid payload format: {line}");
                continue; // Skip to next line
            }

            string timestampStr = parts[0].Trim();
            if (!DateTime.TryParse(timestampStr, out DateTime timestamp))
            {
                Debug.WriteLine($"Failed to parse timestamp: {timestampStr}");
                continue; // Skip to next line
            }

            string numericStr = parts[1].Trim();
            if (!double.TryParse(numericStr, out double numericValue))
            {
                Debug.WriteLine($"Failed to parse numerical value: {numericStr}");
                continue; // Skip to next line
            }

            API_Dates.Add(timestamp);
            API_Magnitude.Add(numericValue);
        }
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
            string selectedChannel = Globals.SelectedChannel;

            if (string.IsNullOrEmpty(selectedChannel))
            {
                MessageBox.Show("Please select a channel from the dropdown.");
                return;
            }

            if (selectedChannel == "All")
            {
                foreach (var channel in voltagesByChannel.Keys.OrderBy(c => c)) // Sort channels in ascending order
                {
                    if (voltagesByChannel[channel].Count == 0 || !timestamps.ContainsKey(channel) || timestamps[channel].Count == 0)
                        continue; // Skip empty channels

                    double[] xs = timestamps[channel].Select(date => date.ToOADate()).ToArray(); // Get timestamps for the current channel
                    double[] ys = voltagesByChannel[channel].ToArray(); // Get voltages for the current channel

                    var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

                    linePlot.Label = $"Channel {channel}";
                    linePlot.LineWidth = 2;
                    linePlot.MarkerSize = 1;
                    linePlot.Color = palette.GetColor(channel); // Use colorIndex for unique colors
                }
            }

            else if (int.TryParse(selectedChannel, out int channelToDisplay))
            {
                if (!voltagesByChannel.ContainsKey(channelToDisplay) || voltagesByChannel[channelToDisplay].Count == 0 ||
                    !timestamps.ContainsKey(channelToDisplay) || timestamps[channelToDisplay].Count == 0)
                {
                    MessageBox.Show($"No data available for Channel {channelToDisplay}.");
                    return;
                }

                double[] xs = timestamps[channelToDisplay].Select(date => date.ToOADate()).ToArray(); // Get timestamps for the current channel
                double[] ys = voltagesByChannel[channelToDisplay].ToArray(); // Get voltages for the current channel

                var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

                linePlot.Label = $"Channel {channelToDisplay}";
                linePlot.LineWidth = 2;
                linePlot.MarkerSize = 1;
                int selectedChannelInt = int.Parse(selectedChannel);
                linePlot.Color = palette.GetColor(selectedChannelInt); // Get a unique color
            }
            else
            {
                MessageBox.Show("Invalid channel selected.");
                return;
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

    public void PlotCorrelationData()
    {
        try
        {

            IPalette palette = new ScottPlot.Palettes.Category10();

            if (Correlation_Dates == null || Correlation_Dates.Count == 0 ||
                Correlation_Magnitude == null || Correlation_Magnitude.Count == 0)
            {
                MessageBox.Show("No correlation data available to plot.");
                return;
            }

            // Convert DateTime to OADate for plotting
            double[] xs = Correlation_Dates.Select(date => date.ToOADate()).ToArray();
            double[] ys = Correlation_Magnitude.ToArray();

            // Add scatter plot
            var scatterPlot = Main_Plot.Plot.Add.Scatter(xs, ys);
            scatterPlot.Label = "Correlation Data";
            scatterPlot.LineWidth = 2;
            scatterPlot.MarkerSize = 3;
            scatterPlot.Color = palette.GetColor(1); // Use colorIndex for unique colors

            // Set plot properties
            Main_Plot.Plot.Legend.IsVisible = true;
            Main_Plot.Plot.Axes.DateTimeTicksBottom();
            Main_Plot.Plot.Axes.Left.Label.Text = "Dot Product Magnitude";
            Main_Plot.Plot.Axes.Bottom.Label.Text = "Timestamp";

            // Auto-scale and refresh
            Main_Plot.Plot.Axes.AutoScale();
            Main_Plot.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting correlation data: {ex.Message}");
        }
    }


    public void PlotAPIDataOverlay()
    {
        try
        {
            IPalette palette = new ScottPlot.Palettes.Category10();

            // Copy the data from API_Dates and API_Magnitude
            API_Correlation_1_Dates = new List<DateTime>(API_Dates);
            API_Correlation_1_Magnitude = new List<double>(API_Magnitude);

            double[] xs = API_Correlation_1_Dates.ConvertAll(date => date.ToOADate()).ToArray();
            double[] ys = API_Correlation_1_Magnitude.ToArray();

            var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

            linePlot.Axes.YAxis = Main_Plot.Plot.Axes.Right;
            linePlot.Label = "API";
            linePlot.LineWidth = 2;
            linePlot.MarkerSize = 1;
            linePlot.Color = palette.GetColor(6); // Get a unique color

            Main_Plot.Plot.Legend.IsVisible = true;
            Main_Plot.Plot.Axes.AutoScale();
            Main_Plot.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting data: {ex.Message}");
        }
    }

    public void PlotAPICorrelationOverlay()
    {
        try
        {
            IPalette palette = new ScottPlot.Palettes.Category10();

            // Copy the data from API_Dates and API_Magnitude
            API_Correlation_2_Dates = new List<DateTime>(API_Dates);
            API_Correlation_2_Magnitude = new List<double>(API_Magnitude);

            double[] xs = API_Correlation_2_Dates.ConvertAll(date => date.ToOADate()).ToArray();
            double[] ys = API_Correlation_2_Magnitude.ToArray();

            var linePlot = Main_Plot.Plot.Add.Scatter(xs, ys);

            linePlot.Axes.YAxis = Main_Plot.Plot.Axes.Left;
            linePlot.Label = "Correlation API";
            linePlot.LineWidth = 2;
            linePlot.MarkerSize = 1;
            linePlot.Color = palette.GetColor(7); // Get a unique color

            Main_Plot.Plot.Legend.IsVisible = true;
            Main_Plot.Plot.Axes.AutoScale();
            Main_Plot.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error plotting data: {ex.Message}");
        }
    }



    public void CalculateDotProductAPI()
    {
        Correlation_Dates.Clear();
        Correlation_Magnitude.Clear();

        // Calculate mean and standard deviation for magnitudes
        double meanApiMagnitude = API_Correlation_1_Magnitude.Average();
        double stdDevApiMagnitude = Math.Sqrt(API_Correlation_1_Magnitude.Average(m => Math.Pow(m - meanApiMagnitude, 2)));

        double meanCorrelationMagnitude = API_Correlation_2_Magnitude.Average();
        double stdDevCorrelationMagnitude = Math.Sqrt(API_Correlation_2_Magnitude.Average(m => Math.Pow(m - meanCorrelationMagnitude, 2)));

        // Normalize the magnitudes using z-score normalization
        var normalizedApiMagnitudes = API_Correlation_1_Magnitude
            .Select(m => (m - meanApiMagnitude) / stdDevApiMagnitude)
            .ToList();

        var normalizedCorrelationMagnitudes = API_Correlation_2_Magnitude
            .Select(m => (m - meanCorrelationMagnitude) / stdDevCorrelationMagnitude)
            .ToList();

        // Debugging output for non-normalized and normalized values
        Debug.WriteLine("Non-Normalized and Normalized API Correlation 1 Magnitudes:");
        for (int i = 0; i < API_Correlation_1_Magnitude.Count; i++)
        {
            Debug.WriteLine($"Original: {API_Correlation_1_Magnitude[i]:F4}, Normalized: {normalizedApiMagnitudes[i]:F4}");
        }

        Debug.WriteLine("Non-Normalized and Normalized API Correlation 2 Magnitudes:");
        for (int i = 0; i < API_Correlation_2_Magnitude.Count; i++)
        {
            Debug.WriteLine($"Original: {API_Correlation_2_Magnitude[i]:F4}, Normalized: {normalizedCorrelationMagnitudes[i]:F4}");
        }

        // Create a dictionary for quick look-up of API_Correlation_Dates and corresponding normalized magnitudes
        var correlationData = API_Correlation_2_Dates
            .Select((date, index) => new { date, magnitude = normalizedCorrelationMagnitudes[index] })
            .ToDictionary(x => x.date, x => x.magnitude);

        foreach (var (date, index) in API_Dates.Select((d, i) => (d, i)))
        {
            if (correlationData.TryGetValue(date, out double correlationMagnitude))
            {
                if (index < normalizedApiMagnitudes.Count)
                {
                    double dotProduct = normalizedApiMagnitudes[index] * correlationMagnitude;
                    Correlation_Dates.Add(date);
                    Correlation_Magnitude.Add(dotProduct);

                    // Debugging output
                    Debug.WriteLine($"Match Found: Date = {date}, Normalized API Magnitude = {normalizedApiMagnitudes[index]:F4}, Normalized Correlation Magnitude = {correlationMagnitude:F4}, Dot Product = {dotProduct:F4}");
                }
                else
                {
                    Debug.WriteLine($"Index out of range: index = {index}, normalizedApiMagnitudes.Count = {normalizedApiMagnitudes.Count}");
                }
            }
            else
            {
                // Debugging output for unmatched dates
                Debug.WriteLine($"No Match Found for Date = {date}");
            }
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
            var apiCorrelationPlot = Main_Plot.Plot.GetPlottables()
                            .OfType<ScottPlot.Plottables.Scatter>()
                            .FirstOrDefault(p => p.Label == "Correlation API");

            if (apiPlot != null)
            {
                Main_Plot.Plot.Remove(apiPlot);
                Main_Plot.Plot.Remove(apiCorrelationPlot);
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
            API_Magnitude.Add(data.Illumination); 
            Console.WriteLine($"{date.ToShortDateString()} - {data.PhaseName} ({data.ZodiacName}) - Age: {data.Age:F2} days - Illumination: {data.Illumination:P0}");
        }

        PlotLollipopData(API_Dates, API_Magnitude);
    }

    public void CalculateDotProduct()
    {
        Correlation_Dates.Clear();
        Correlation_Magnitude.Clear();

        // Calculate mean and standard deviation for magnitudes
        double meanApiMagnitude = API_Correlation_1_Magnitude.Average();
        double stdDevApiMagnitude = Math.Sqrt(API_Correlation_1_Magnitude.Average(m => Math.Pow(m - meanApiMagnitude, 2)));

        double meanCorrelationMagnitude = voltagesByChannel[1].Average();
        double stdDevCorrelationMagnitude = Math.Sqrt(voltagesByChannel[1].Average(m => Math.Pow(m - meanCorrelationMagnitude, 2)));

        // Normalize the magnitudes using z-score normalization
        var normalizedApiMagnitudes = API_Correlation_1_Magnitude
            .Select(m => (m - meanApiMagnitude) / stdDevApiMagnitude)
            .ToList();

        var normalizedCorrelationMagnitudes = voltagesByChannel[1]
            .Select(m => (m - meanCorrelationMagnitude) / stdDevCorrelationMagnitude)
            .ToList();

        // Debugging output for non-normalized and normalized values
        Debug.WriteLine("Non-Normalized and Normalized API Correlation 1 Magnitudes:");
        for (int i = 0; i < API_Correlation_1_Magnitude.Count; i++)
        {
            // Debug.WriteLine($"Original: {API_Correlation_1_Magnitude[i]:F4}, Normalized: {normalizedApiMagnitudes[i]:F4}");
        }

        Debug.WriteLine("Non-Normalized and Normalized API Correlation 2 Magnitudes:");
        for (int i = 0; i < voltagesByChannel[1].Count; i++)
        {
            // Debug.WriteLine($"Normalized: {normalizedCorrelationMagnitudes[i]:F4}");
        }

        // Create a dictionary for quick look-up of API_Correlation_Dates and corresponding normalized magnitudes
        var correlationData = _Dates[1]
            .Select((date, index) => new { date, magnitude = normalizedCorrelationMagnitudes[index] })
            .GroupBy(x => x.date) // Group by date to handle duplicates
            .ToDictionary(
                g => g.Key, // Use the date as the key
                g => g.Average(x => x.magnitude) // Average magnitudes for duplicate dates
             );


        foreach (var (date, index) in API_Dates.Select((d, i) => (d, i)))
        {
            if (correlationData.TryGetValue(date, out double correlationMagnitude))
            {
                if (index < normalizedApiMagnitudes.Count)
                {
                    double dotProduct = normalizedApiMagnitudes[index] * correlationMagnitude;
                    Correlation_Dates.Add(date);
                    Correlation_Magnitude.Add(dotProduct);

                    // Debugging output
                    // Debug.WriteLine($"Match Found: Date = {date}, Normalized API Magnitude = {normalizedApiMagnitudes[index]:F4}, Normalized Correlation Magnitude = {correlationMagnitude:F4}, Dot Product = {dotProduct:F4}");
                }
                else
                {
                    // Debug.WriteLine($"Index out of range: index = {index}, normalizedApiMagnitudes.Count = {normalizedApiMagnitudes.Count}");
                }
            }
            else
            {
                // Debugging output for unmatched dates
                Debug.WriteLine($"No Match Found for Date = {date}");
            }
        }

    }

}