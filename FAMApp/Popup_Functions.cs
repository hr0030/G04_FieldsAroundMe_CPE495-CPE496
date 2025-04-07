using ScottPlot.WinForms;

public class Popup_Functions
{

    private FormsPlot Main_Plot;
    private FormsPlot API_Plot;
    private Control originalParent; // Store the original parent container

    public Popup_Functions(FormsPlot mainPlot, FormsPlot apiPlot)
    {
        this.Main_Plot = mainPlot;
        this.API_Plot = apiPlot;
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
                return datePicker.Value.ToString("yyyy_MM_dd"); // Format: d_m_y
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
                return return_date_string; // Format: d_m_y
            }
        }
        return null; // If the user cancels the selection
    }

    public void spawnAPIPopup(string yAxisLabel)
    {
        // Check if API_Plot already has a parent
        if (API_Plot.Parent != null)
        {
            originalParent = API_Plot.Parent; // Store the original parent
            originalParent.Controls.Remove(API_Plot); // Remove it from the parent
        }

        // Create a new Form for the pop-out window
        Form popOutForm = new Form
        {
            Text = "New Plot Window",
            Size = new Size(500, 400)
        };

        // Clear the plot before displaying the new form
        API_Plot.Plot.Clear();
        API_Plot.Refresh();

        // Add API_Plot to the new form
        popOutForm.Controls.Add(API_Plot);
        API_Plot.Dock = DockStyle.Fill;

        // Configure plot labels
        API_Plot.Plot.Axes.DateTimeTicksBottom();
        API_Plot.Plot.Axes.Bottom.Label.Text = "Date and Time";
        API_Plot.Plot.Axes.Left.Label.Text = yAxisLabel;
        API_Plot.Refresh();

        // Handle the form closing event to restore API_Plot
        popOutForm.FormClosing += (s, e) =>
        {
            if (originalParent != null)
            {
                originalParent.Controls.Add(API_Plot);
                API_Plot.Dock = DockStyle.Fill; // Restore layout
                API_Plot.Refresh();
            }
        };

        // Show the pop-out window
        popOutForm.Show();
    }

}