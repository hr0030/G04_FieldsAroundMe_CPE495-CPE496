namespace FAMApp
{
    partial class Settings_Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            Save_Settings_Button = new Button();
            IP_Address_Textbox = new TextBox();
            label2 = new Label();
            Sampling_Frequency_Textbox = new TextBox();
            Latitude_Textbox = new TextBox();
            label3 = new Label();
            Sensor_Name_Textbox = new TextBox();
            label4 = new Label();
            Sensor_Number_Textbox = new TextBox();
            label5 = new Label();
            Longitude_Textbox = new TextBox();
            label6 = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 15);
            label1.Name = "label1";
            label1.Size = new Size(62, 15);
            label1.TabIndex = 0;
            label1.Text = "IP Address";
            // 
            // Save_Settings_Button
            // 
            Save_Settings_Button.Location = new Point(12, 375);
            Save_Settings_Button.Name = "Save_Settings_Button";
            Save_Settings_Button.Size = new Size(535, 23);
            Save_Settings_Button.TabIndex = 1;
            Save_Settings_Button.Text = "Save Settings";
            Save_Settings_Button.UseVisualStyleBackColor = true;
            Save_Settings_Button.Click += Save_Settings_Button_Click;
            // 
            // IP_Address_Textbox
            // 
            IP_Address_Textbox.Location = new Point(226, 12);
            IP_Address_Textbox.Name = "IP_Address_Textbox";
            IP_Address_Textbox.Size = new Size(321, 23);
            IP_Address_Textbox.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 73);
            label2.Name = "label2";
            label2.Size = new Size(115, 15);
            label2.TabIndex = 3;
            label2.Text = "Sampling Frequency";
            // 
            // Sampling_Frequency_Textbox
            // 
            Sampling_Frequency_Textbox.Location = new Point(226, 70);
            Sampling_Frequency_Textbox.Name = "Sampling_Frequency_Textbox";
            Sampling_Frequency_Textbox.Size = new Size(321, 23);
            Sampling_Frequency_Textbox.TabIndex = 4;
            // 
            // Latitude_Textbox
            // 
            Latitude_Textbox.Location = new Point(226, 260);
            Latitude_Textbox.Name = "Latitude_Textbox";
            Latitude_Textbox.Size = new Size(321, 23);
            Latitude_Textbox.TabIndex = 8;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 263);
            label3.Name = "label3";
            label3.Size = new Size(50, 15);
            label3.TabIndex = 7;
            label3.Text = "Latitude";
            // 
            // Sensor_Name_Textbox
            // 
            Sensor_Name_Textbox.Location = new Point(226, 197);
            Sensor_Name_Textbox.Name = "Sensor_Name_Textbox";
            Sensor_Name_Textbox.Size = new Size(321, 23);
            Sensor_Name_Textbox.TabIndex = 6;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 200);
            label4.Name = "label4";
            label4.Size = new Size(77, 15);
            label4.TabIndex = 5;
            label4.Text = "Sensor Name";
            // 
            // Sensor_Number_Textbox
            // 
            Sensor_Number_Textbox.Location = new Point(226, 132);
            Sensor_Number_Textbox.Name = "Sensor_Number_Textbox";
            Sensor_Number_Textbox.Size = new Size(321, 23);
            Sensor_Number_Textbox.TabIndex = 10;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(12, 135);
            label5.Name = "label5";
            label5.Size = new Size(89, 15);
            label5.TabIndex = 9;
            label5.Text = "Sensor Number";
            // 
            // Longitude_Textbox
            // 
            Longitude_Textbox.Location = new Point(226, 321);
            Longitude_Textbox.Name = "Longitude_Textbox";
            Longitude_Textbox.Size = new Size(321, 23);
            Longitude_Textbox.TabIndex = 12;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(12, 324);
            label6.Name = "label6";
            label6.Size = new Size(61, 15);
            label6.TabIndex = 11;
            label6.Text = "Longitude";
            // 
            // Settings_Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(559, 407);
            Controls.Add(Longitude_Textbox);
            Controls.Add(label6);
            Controls.Add(Sensor_Number_Textbox);
            Controls.Add(label5);
            Controls.Add(Latitude_Textbox);
            Controls.Add(label3);
            Controls.Add(Sensor_Name_Textbox);
            Controls.Add(label4);
            Controls.Add(Sampling_Frequency_Textbox);
            Controls.Add(label2);
            Controls.Add(IP_Address_Textbox);
            Controls.Add(Save_Settings_Button);
            Controls.Add(label1);
            Name = "Settings_Form";
            Text = "Settings";
            Load += Settings_Form_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Button Save_Settings_Button;
        private TextBox IP_Address_Textbox;
        private Label label2;
        private TextBox Sampling_Frequency_Textbox;
        private TextBox Latitude_Textbox;
        private Label label3;
        private TextBox Sensor_Name_Textbox;
        private Label label4;
        private TextBox Sensor_Number_Textbox;
        private Label label5;
        private TextBox Longitude_Textbox;
        private Label label6;
    }
}