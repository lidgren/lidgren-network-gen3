using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Lidgren.Network;
using SamplesCommon;

namespace ImageClient
{
	public partial class Form1 : Form
	{
		private NetPeer m_dummyPeer;
		private NetPeerConfiguration m_config;

		public Form1()
		{
			m_config = new NetPeerConfiguration("ImageTransfer");
			m_dummyPeer = new NetPeer(m_config);
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Program.SpawnGetter(textBox1.Text, m_config);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			NetPeerSettingsWindow win = new NetPeerSettingsWindow("Client settings", m_dummyPeer);
			win.Show();
		}
	}
}
