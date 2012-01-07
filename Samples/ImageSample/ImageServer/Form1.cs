using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SamplesCommon;

namespace ImageServer
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
				m_settingsWindow = new NetPeerSettingsWindow("Image server settings", Program.Server);
				m_settingsWindow.Show();
			}
			else
			{
				m_settingsWindow.Close();
				m_settingsWindow = null;
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Image files|*.png;*.jpg;*.jpeg";
			DialogResult res = dlg.ShowDialog();
			if (res != DialogResult.OK)
				return;
			Program.Start(dlg.FileName);
		}
	}
}
