using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SamplesCommon;

namespace ChatClient
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			textBox1.KeyDown += new KeyEventHandler(textBox1_KeyDown);
		}

		void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
			{
				string txt = textBox1.Text.Trim();
				Program.Input(txt);
				textBox1.Text = "";
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (Program.SettingsWindow == null || Program.SettingsWindow.IsDisposed)
				Program.SettingsWindow = new NetPeerSettingsWindow("Client settings", Program.Client);
			if (Program.SettingsWindow.Visible)
				Program.SettingsWindow.Hide();
			else
				Program.SettingsWindow.Show();
		}
	}
}
