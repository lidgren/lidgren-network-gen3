using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

namespace ChatServer
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (button1.Text == "Start")
			{
				Program.StartServer();
				button1.Text = "Shut down";
			}
			else
			{
				Program.Shutdown();
				button1.Text = "Start";
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Program.DisplaySettings();
		}
	}
}
