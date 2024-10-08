﻿using LiveChartsCore; // Core library
using LiveChartsCore.SkiaSharpView; // Core components
using LiveChartsCore.SkiaSharpView.WinForms; // For WinForms components
using System.Drawing;
using System.Windows.Forms;

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
            cartesianChart1 = new CartesianChart();
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
        private CartesianChart cartesianChart1;
    }
}
