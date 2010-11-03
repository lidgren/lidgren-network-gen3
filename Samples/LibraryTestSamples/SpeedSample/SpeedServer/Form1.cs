using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SpeedTestServer
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Program.DisplaySettings();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (button2.Text == "Start")
			{
				Program.Start();
				button2.Text = "Shutdown";
			}
			else
			{
				Program.Shutdown();
				button2.Text = "Start";
			}
		}
	}
}
