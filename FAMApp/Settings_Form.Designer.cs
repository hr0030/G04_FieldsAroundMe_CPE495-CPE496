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
            Save_Settings_Button.Location = new Point(13, 114);
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
            // Settings_Form
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(559, 152);
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
    }
}