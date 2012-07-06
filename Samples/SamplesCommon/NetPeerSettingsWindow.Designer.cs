namespace SamplesCommon
{
	partial class NetPeerSettingsWindow
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
			this.DebugCheckBox = new System.Windows.Forms.CheckBox();
			this.VerboseCheckBox = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.DupesTextBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.MaxLatencyTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.LossTextBox = new System.Windows.Forms.TextBox();
			this.MinLatencyTextBox = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label7 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.PingFrequencyTextBox = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.StatisticsLabel = new System.Windows.Forms.Label();
			this.button2 = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// DebugCheckBox
			// 
			this.DebugCheckBox.AutoSize = true;
			this.DebugCheckBox.Location = new System.Drawing.Point(6, 21);
			this.DebugCheckBox.Name = "DebugCheckBox";
			this.DebugCheckBox.Size = new System.Drawing.Size(153, 17);
			this.DebugCheckBox.TabIndex = 0;
			this.DebugCheckBox.Text = "Display Debug messages";
			this.DebugCheckBox.UseVisualStyleBackColor = true;
			// 
			// VerboseCheckBox
			// 
			this.VerboseCheckBox.AutoSize = true;
			this.VerboseCheckBox.Location = new System.Drawing.Point(6, 44);
			this.VerboseCheckBox.Name = "VerboseCheckBox";
			this.VerboseCheckBox.Size = new System.Drawing.Size(197, 17);
			this.VerboseCheckBox.TabIndex = 1;
			this.VerboseCheckBox.Text = "Display Verbose debug messages";
			this.VerboseCheckBox.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.label9);
			this.groupBox1.Controls.Add(this.DupesTextBox);
			this.groupBox1.Controls.Add(this.label5);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.MaxLatencyTextBox);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.LossTextBox);
			this.groupBox1.Controls.Add(this.MinLatencyTextBox);
			this.groupBox1.Location = new System.Drawing.Point(291, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(330, 110);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Simulation";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(163, 80);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(16, 13);
			this.label8.TabIndex = 10;
			this.label8.Text = "%";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(6, 80);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(61, 13);
			this.label9.TabIndex = 9;
			this.label9.Text = "Duplicates";
			// 
			// DupesTextBox
			// 
			this.DupesTextBox.Location = new System.Drawing.Point(103, 77);
			this.DupesTextBox.Name = "DupesTextBox";
			this.DupesTextBox.Size = new System.Drawing.Size(54, 22);
			this.DupesTextBox.TabIndex = 8;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(163, 52);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(16, 13);
			this.label5.TabIndex = 7;
			this.label5.Text = "%";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(247, 24);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(21, 13);
			this.label4.TabIndex = 6;
			this.label4.Text = "ms";
			// 
			// MaxLatencyTextBox
			// 
			this.MaxLatencyTextBox.Location = new System.Drawing.Point(180, 21);
			this.MaxLatencyTextBox.Name = "MaxLatencyTextBox";
			this.MaxLatencyTextBox.Size = new System.Drawing.Size(54, 22);
			this.MaxLatencyTextBox.TabIndex = 5;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(163, 24);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(11, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "-";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 52);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(29, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Loss";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 24);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(91, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "One way latency";
			// 
			// LossTextBox
			// 
			this.LossTextBox.Location = new System.Drawing.Point(103, 49);
			this.LossTextBox.Name = "LossTextBox";
			this.LossTextBox.Size = new System.Drawing.Size(54, 22);
			this.LossTextBox.TabIndex = 1;
			// 
			// MinLatencyTextBox
			// 
			this.MinLatencyTextBox.Location = new System.Drawing.Point(103, 21);
			this.MinLatencyTextBox.Name = "MinLatencyTextBox";
			this.MinLatencyTextBox.Size = new System.Drawing.Size(54, 22);
			this.MinLatencyTextBox.TabIndex = 0;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label7);
			this.groupBox2.Controls.Add(this.DebugCheckBox);
			this.groupBox2.Controls.Add(this.VerboseCheckBox);
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.PingFrequencyTextBox);
			this.groupBox2.Location = new System.Drawing.Point(12, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(273, 110);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Settings";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(182, 70);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(21, 13);
			this.label7.TabIndex = 10;
			this.label7.Text = "ms";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(6, 70);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(84, 13);
			this.label6.TabIndex = 9;
			this.label6.Text = "Ping frequency";
			// 
			// PingFrequencyTextBox
			// 
			this.PingFrequencyTextBox.Location = new System.Drawing.Point(98, 67);
			this.PingFrequencyTextBox.Name = "PingFrequencyTextBox";
			this.PingFrequencyTextBox.Size = new System.Drawing.Size(76, 22);
			this.PingFrequencyTextBox.TabIndex = 8;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(524, 409);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(101, 36);
			this.button1.TabIndex = 5;
			this.button1.Text = "Save";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.StatisticsLabel);
			this.groupBox3.Location = new System.Drawing.Point(12, 128);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(609, 275);
			this.groupBox3.TabIndex = 6;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Statistics";
			// 
			// StatisticsLabel
			// 
			this.StatisticsLabel.AutoSize = true;
			this.StatisticsLabel.Location = new System.Drawing.Point(6, 22);
			this.StatisticsLabel.Name = "StatisticsLabel";
			this.StatisticsLabel.Size = new System.Drawing.Size(79, 13);
			this.StatisticsLabel.TabIndex = 0;
			this.StatisticsLabel.Text = "StatisticsLabel";
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.Location = new System.Drawing.Point(419, 409);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(99, 36);
			this.button2.TabIndex = 7;
			this.button2.Text = "Close";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// NetPeerSettingsWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(633, 453);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "NetPeerSettingsWindow";
			this.Text = "NetPeerSettingsWindow1";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.CheckBox DebugCheckBox;
		public System.Windows.Forms.CheckBox VerboseCheckBox;
		public System.Windows.Forms.TextBox LossTextBox;
		public System.Windows.Forms.TextBox MinLatencyTextBox;
		public System.Windows.Forms.TextBox MaxLatencyTextBox;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox PingFrequencyTextBox;
		private System.Windows.Forms.GroupBox groupBox3;
		public System.Windows.Forms.Label StatisticsLabel;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		public System.Windows.Forms.TextBox DupesTextBox;
	}
}