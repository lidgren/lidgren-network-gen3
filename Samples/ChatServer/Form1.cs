using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SamplesCommon;

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
			if (Program.SettingsWindow == null || Program.SettingsWindow.IsDisposed)
				Program.SettingsWindow = new NetPeerSettingsWindow("Client settings", Program.Server);
			if (Program.SettingsWindow.Visible)
				Program.SettingsWindow.Hide();
			else
				Program.SettingsWindow.Show();
		}
	}
}
