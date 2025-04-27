using ScottPlot.WinForms;
using static Parse_Graph_Functions;
public class Popup_Functions
{

    private FormsPlot Main_Plot;
    private FormsPlot API_Plot;
    private Control originalParent;
    Parse_Graph_Functions parse_graph;
    public Popup_Functions(FormsPlot mainPlot, FormsPlot apiPlot, Parse_Graph_Functions parsegraph)
    {
        this.Main_Plot = mainPlot;
        this.API_Plot = apiPlot;
        this.parse_graph = parsegraph;
    }

    public string ShowSingleDatePickerDialog()
    {
        using (Form dateForm = new Form())
        {
            dateForm.Text = "Date Picker";
            dateForm.Size = new Size(250, 200);
            dateForm.StartPosition = FormStartPosition.CenterScreen;

            DateTimePicker datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(30, 30),
                Width = 150
            };

            Button confirmButton = new Button
            {
                Text = "OK",
                Location = new Point(75, 80),
                DialogResult = DialogResult.OK
            };

            dateForm.Controls.Add(datePicker);
            dateForm.Controls.Add(confirmButton);
            dateForm.AcceptButton = confirmButton;

            if (dateForm.ShowDialog() == DialogResult.OK)
            {
                return datePicker.Value.ToString("yyyy_MM_dd"); 
            }
        }
        return null; // If the user cancels the selection
    }

    public string ShowDoubleDatePickerDialog()
    {
        using (Form dateForm = new Form())
        {
            dateForm.Text = "Date Picker";
            dateForm.Size = new Size(250, 200);
            dateForm.StartPosition = FormStartPosition.CenterScreen;

            DateTimePicker startDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(30, 30),
                Width = 150
            };

            DateTimePicker endDatePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(30, 60),
                Width = 150
            };

            Button confirmButton = new Button
            {
                Text = "OK",
                Location = new Point(75, 110),
                DialogResult = DialogResult.OK
            };

            dateForm.Controls.Add(startDatePicker);
            dateForm.Controls.Add(endDatePicker);
            dateForm.Controls.Add(confirmButton);
            dateForm.AcceptButton = confirmButton;

            if (dateForm.ShowDialog() == DialogResult.OK)
            {
                if (startDatePicker.Value > endDatePicker.Value)
                {
                    MessageBox.Show("Error: End Date is Before Start Date", "Date Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                string return_date_string = startDatePicker.Value.ToString("yyyy_MM_dd") + "," + endDatePicker.Value.ToString("yyyy_MM_dd");
                return return_date_string; 
            }
        }
        return null; // If the user cancels the selection
    }

    public void spawnAPIPopup(string yAxisLabel)
    {
        if (API_Plot.Parent != null)
        {
            originalParent = API_Plot.Parent;
            originalParent.Controls.Remove(API_Plot); 
        }

        Form popOutForm = new Form
        {
            Text = "API Plot",
            Size = new Size(500, 400)
        };

        Panel panel = new Panel
        {
            Dock = DockStyle.Fill
        };

        Button overlayAPIButton = new Button
        {
            Text = "Overlay API on Main Graph",
            AutoSize = true,
            Anchor = AnchorStyles.Bottom,
            Padding = new Padding(10),
            Margin = new Padding(10)
        };

        overlayAPIButton.Click += (s, e) =>
        {
            parse_graph.PlotAPIDataOverlay();
        };

        overlayAPIButton.Dock = DockStyle.Bottom;
        API_Plot.Plot.Clear();
        API_Plot.Refresh();
        API_Plot.Dock = DockStyle.Fill;
        panel.Controls.Add(API_Plot);
        panel.Controls.Add(overlayAPIButton);
        popOutForm.Controls.Add(panel);
        API_Plot.Plot.Axes.DateTimeTicksBottom();
        API_Plot.Plot.Axes.Bottom.Label.Text = "Date and Time";
        API_Plot.Plot.Axes.Left.Label.Text = yAxisLabel;
        API_Plot.Refresh();

        popOutForm.FormClosing += (s, e) =>
        {
            if (originalParent != null)
            {
                originalParent.Controls.Add(API_Plot);
                API_Plot.Plot.Clear(); 
                API_Plot.Refresh();    
                API_Plot.Dock = DockStyle.Fill;
            }
        };


        popOutForm.Show();
    }

    public static bool spawnLoginPopup()
    {
        // Create form
        Form loginForm = new Form()
        {
            Width = 325,
            Height = 200,
            Text = "Login",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false
        };

        // Username label and textbox
        Label userLabel = new Label() { Left = 20, Top = 20, Text = "Username" };
        TextBox userBox = new TextBox() { Left = 125, Top = 20, Width = 150 };

        // Password label and textbox
        Label passLabel = new Label() { Left = 20, Top = 60, Text = "Password" };
        TextBox passBox = new TextBox() { Left = 125, Top = 60, Width = 150, UseSystemPasswordChar = true };

        // OK button
        Button okButton = new Button() { Text = "Login", Left = 100, Width = 80, Top = 100, DialogResult = DialogResult.OK };
        loginForm.AcceptButton = okButton;

        // Add controls
        loginForm.Controls.Add(userLabel);
        loginForm.Controls.Add(userBox);
        loginForm.Controls.Add(passLabel);
        loginForm.Controls.Add(passBox);
        loginForm.Controls.Add(okButton);

        // Show dialog
        if (loginForm.ShowDialog() == DialogResult.OK)
        {
            string username = userBox.Text;
            string password = passBox.Text;

            if (username == "admin" && password == "password")
                return true;
        }

        return false;
    }


}