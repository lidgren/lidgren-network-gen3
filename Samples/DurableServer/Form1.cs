using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SamplesCommon;

namespace DurableServer
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
			if (m_settingsWindow == null)
			{
				m_settingsWindow = new NetPeerSettingsWindow("Durable server settings", Program.Server);
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
