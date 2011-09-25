using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Lidgren.Network;

namespace ManyClients
{
	public partial class Client : Form
	{
		private const double c_sendFrequency = 1.0;

		public NetClient Net;

		private double m_lastSent;

		public Client()
		{
			InitializeComponent();

			NetPeerConfiguration config = new NetPeerConfiguration("many");
#if DEBUG
			config.SimulatedLoss = 0.02f;
#endif
			Net = new NetClient(config);
			Net.Start();
			Net.Connect("localhost", 14242);
		}

		protected override void OnClosed(EventArgs e)
		{
			Net.Shutdown("closed");
		}

		internal void Shutdown()
		{
			Net.Shutdown("bye");
		}

		internal void Heartbeat()
		{
			NetIncomingMessage inc;
			while ((inc = Net.ReadMessage()) != null)
			{
				switch (inc.MessageType)
				{
					case NetIncomingMessageType.StatusChanged:
						NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
						this.Text = status.ToString();
						break;
					case NetIncomingMessageType.ErrorMessage:
						this.Text = inc.ReadString();
						break;
				}
			}

			// send message?
			if (NetTime.Now > m_lastSent + c_sendFrequency)
			{
				var om = Net.CreateMessage();
				om.Write("Hi!");
				Net.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
				m_lastSent = NetTime.Now;

				// also update title
#if DEBUG
				this.Text = Net.Statistics.SentBytes + " bytes sent; " + Net.Statistics.ReceivedBytes + " bytes received";
#else
				string str = Net.ServerConnection == null ? "No connection" : Net.ServerConnection.Status.ToString();
				if (this.Text != str)
					this.Text = str;
#endif
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			var om = Net.CreateMessage();
			om.Write("Manual hi!");

			Net.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
		}
	}
}
