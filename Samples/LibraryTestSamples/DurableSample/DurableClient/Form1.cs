using System;
using System.Windows.Forms;

using SamplesCommon;

namespace DurableClient
{
	public partial class Form1 : Form
	{
		private NetPeerSettingsWindow m_settingsWindow;

		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(textBox1.Text))
				textBox1.Text = "localhost";

			Program.Connect(textBox1.Text);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (m_settingsWindow == null)
			{
				m_settingsWindow = new NetPeerSettingsWindow("Durable client settings", Program.Client);
				m_settingsWindow.Show();
			}
			else
			{
				m_settingsWindow.Close();
				m_settingsWindow = null;
			}
		}
	}
}
