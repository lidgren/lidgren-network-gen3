using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

using Lidgren.Network;

namespace SamplesCommon
{
	public partial class NetPeerSettingsWindow : Form
	{
		public NetPeer Peer;
		public Timer timer;

		public NetPeerSettingsWindow(string title, NetPeer peer)
		{
			Peer = peer;
			InitializeComponent();
			UpdateLabelsAndBoxes();
			RefreshData();
			this.Text = title;

			// auto refresh now and then
			timer = new Timer();
			timer.Interval = 250;
			timer.Tick += new EventHandler(timer_Tick);
			timer.Enabled = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			timer.Enabled = false;
			base.OnClosed(e);
		}

		void timer_Tick(object sender, EventArgs e)
		{
			RefreshData();
		}

		private void UpdateLabelsAndBoxes()
		{
			var pc = Peer.Configuration;

#if DEBUG
			var loss = (pc.SimulatedLoss * 100.0f).ToString();
			label5.Text = loss + " %";
			LossTextBox.Text = loss;

			var dupes = (pc.SimulatedDuplicatesChance * 100.0f).ToString();
			label8.Text = dupes + " %";
			DupesTextBox.Text = dupes;

			var minLat = (pc.SimulatedMinimumLatency * 1000.0f).ToString();
			var maxLat = ((pc.SimulatedMinimumLatency + pc.SimulatedRandomLatency) * 1000.0f).ToString();
#else
			var minLat = "";
			var maxLat = "";
#endif
			label4.Text = minLat + " to " + maxLat + " ms";
			MinLatencyTextBox.Text = minLat;
			MaxLatencyTextBox.Text = maxLat;

			DebugCheckBox.Checked = Peer.Configuration.IsMessageTypeEnabled(NetIncomingMessageType.DebugMessage);
			VerboseCheckBox.Checked = Peer.Configuration.IsMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage);
			PingFrequencyTextBox.Text = (Peer.Configuration.PingInterval * 1000).ToString();
		}

		private void RefreshData()
		{
			StringBuilder bdr = new StringBuilder();
			bdr.AppendLine(Peer.Statistics.ToString());

			if (Peer.ConnectionsCount > 0)
			{
				NetConnection conn = Peer.Connections[0];
				bdr.AppendLine("Connection 0:");
				bdr.Append(conn.Statistics.ToString());
			}

			StatisticsLabel.Text = bdr.ToString();
		}

		private void Save()
		{
			Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.DebugMessage, DebugCheckBox.Checked);
			Peer.Configuration.SetMessageTypeEnabled(NetIncomingMessageType.VerboseDebugMessage, VerboseCheckBox.Checked);
#if DEBUG
			float f;
			if (Single.TryParse(LossTextBox.Text, out f))
				Peer.Configuration.SimulatedLoss = (float)((double)f / 100.0);
			if (Single.TryParse(DupesTextBox.Text, out f))
				Peer.Configuration.SimulatedDuplicatesChance = (float)((double)f / 100.0);
			if (float.TryParse(MinLatencyTextBox.Text, out f))
				Peer.Configuration.SimulatedMinimumLatency = (float)(f / 1000.0);
			if (float.TryParse(PingFrequencyTextBox.Text, out f))
				Peer.Configuration.PingInterval = (float)(f / 1000.0);
			float max;
			if (float.TryParse(MaxLatencyTextBox.Text, out max))
			{
				max = (float)((double)max / 1000.0);
				float r = max - Peer.Configuration.SimulatedMinimumLatency;
				if (r > 0)
				{
					Peer.Configuration.SimulatedRandomLatency = r;
					double nm = (double)Peer.Configuration.SimulatedMinimumLatency + (double)Peer.Configuration.SimulatedRandomLatency;
					MaxLatencyTextBox.Text = ((int)(max * 1000)).ToString();
				}
			}
#endif

		}

		private void button1_Click(object sender, EventArgs e)
		{
			Save();
			UpdateLabelsAndBoxes();
			RefreshData();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
