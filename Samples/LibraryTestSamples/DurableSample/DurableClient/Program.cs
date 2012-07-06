using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Threading;
using Lidgren.Network;
using SamplesCommon;
using System.Text;

namespace DurableClient
{
	static class Program
	{
		public static Form1 MainForm;
		public static NetClient Client;

		private static bool s_sendStuff;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("durable");
			config.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
			Client = new NetClient(config);
			Client.Start();

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);

			Client.Shutdown("App exiting");
		}

		public static void Display(string text)
		{
			NativeMethods.AppendText(MainForm.richTextBox1, text);
		}

		private static double s_nextSendReliableOrdered;
		private static uint[] s_reliableOrderedNr = new uint[3];

		private static double s_nextSendSequenced;
		private static uint[] s_sequencedNr = new uint[3];

		private static double s_lastLabelUpdate;
		private const double kLabelUpdateFrequency = 0.25;

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage msg;
				while ((msg = Client.WaitMessage(1)) != null)
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							Display(msg.ReadString());
							break;
						case NetIncomingMessageType.Data:
							Display("Received data?!");
							break;
						case NetIncomingMessageType.DiscoveryResponse:
							Display("Got discovery response from " + msg.SenderEndPoint);
							NetOutgoingMessage approval = Client.CreateMessage();
							approval.Write("durableschmurable");
							Client.Connect(msg.SenderEndPoint, approval);
							break;

						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
							string reason = msg.ReadString();
							Display("New status: " + status + " (" + reason + ")");

							if (status == NetConnectionStatus.Connected)
							{
								// go
								s_sendStuff = true;
							}
							break;
					}
					Client.Recycle(msg);
				}

				if (s_sendStuff)
				{
					double now = NetTime.Now;

					float speed = 1.0f;

					float speedMultiplier = 1.0f / speed;

					int r = NetRandom.Instance.Next(3);
					if (now > s_nextSendReliableOrdered)
					{
						NetOutgoingMessage om = Client.CreateMessage(5);

						uint rv = s_reliableOrderedNr[r];
						s_reliableOrderedNr[r]++;

						om.Write(rv);

						Client.SendMessage(om, NetDeliveryMethod.ReliableOrdered, r);
						s_nextSendReliableOrdered = now + (NetRandom.Instance.NextSingle() * (0.01f * speedMultiplier)) + (0.005f * speedMultiplier);
					}

					if (now > s_nextSendSequenced)
					{
						NetOutgoingMessage om = Client.CreateMessage();

						uint v = s_sequencedNr[r];
						s_sequencedNr[r]++;
						om.Write(v);
						Client.SendMessage(om, NetDeliveryMethod.UnreliableSequenced, r);
						s_nextSendSequenced = now + (NetRandom.Instance.NextSingle() * (0.01f * speedMultiplier)) + (0.005f * speedMultiplier);
					}

					if (now > s_lastLabelUpdate + kLabelUpdateFrequency)
					{
						UpdateLabel();
						s_lastLabelUpdate = now;
					}
				}
			}
		}

		private static void UpdateLabel()
		{
			NetConnection conn = Client.ServerConnection;
			if (conn != null)
			{
				StringBuilder bdr = new StringBuilder();
				bdr.Append(Client.Statistics.ToString());
				bdr.Append(conn.Statistics.ToString());

				bdr.AppendLine("SENT Reliable ordered: " + s_reliableOrderedNr[0] + ", " + s_reliableOrderedNr[1] + ", " + s_reliableOrderedNr[2]);
				bdr.AppendLine("SENT Sequenced: " + s_sequencedNr[0] + ", " + s_sequencedNr[1] + ", " + s_sequencedNr[2]);
				// bdr.AppendLine("Unsent bytes: " + conn.q);
				MainForm.label1.Text = bdr.ToString();
			}
		}

		public static void Connect(string host)
		{
			//NetOutgoingMessage approval = Client.CreateMessage();
			//approval.Write("durableschmurable");
			// Client.Connect(host, 14242, approval);
			Client.DiscoverKnownPeer(host, 14242);
		}
	}
}
