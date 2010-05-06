using System;
using System.Threading;
using System.Windows.Forms;

using Lidgren.Network;

using SamplesCommon;
using System.Text;

namespace DurableServer
{
	static class Program
	{
		public static Form1 MainForm;
		public static NetServer Server;
		
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("durable");
			config.Port = 14242;
			config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
			Server = new NetServer(config);
			Server.Start();

			m_expectedReliableOrdered = new uint[3];
			m_reliableOrderedCorrect = new int[3];
			m_reliableOrderedErrors = new int[3];

			m_expectedSequenced = new uint[3];
			m_sequencedCorrect = new int[3];
			m_sequencedErrors = new int[3];

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);

			Server.Shutdown("App exiting");
		}

		private static void Display(string text)
		{
			NativeMethods.AppendText(MainForm.richTextBox1, text);
		}

		private static double m_lastLabelUpdate;
		private const double kLabelUpdateFrequency = 0.25;

		private static uint[] m_expectedReliableOrdered;
		private static int[] m_reliableOrderedCorrect;
		private static int[] m_reliableOrderedErrors;

		private static uint[] m_expectedSequenced;
		private static int[] m_sequencedCorrect;
		private static int[] m_sequencedErrors;

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage msg;
				while ((msg = Server.ReadMessage()) != null)
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							Display(msg.ReadString());
							break;
						case NetIncomingMessageType.ConnectionApproval:
							string ok = msg.ReadString();
							if (ok == "durableschmurable")
								msg.SenderConnection.Approve();
							else
								msg.SenderConnection.Deny("You didn't say the secret word!");
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
							string reason = msg.ReadString();
							Display("New status: " + status + " (" + reason + ")");
							break;
						case NetIncomingMessageType.Data:
							uint nr = msg.ReadUInt32();
							int chan = msg.SequenceChannel;
							switch (msg.DeliveryMethod)
							{
								case NetDeliveryMethod.ReliableOrdered:
									if (nr != m_expectedReliableOrdered[chan])
									{
										m_reliableOrderedErrors[chan]++;
										m_expectedReliableOrdered[chan] = nr + 1;
									}
									else
									{
										m_reliableOrderedCorrect[chan]++;
										m_expectedReliableOrdered[chan]++;
									}
									break;
								case NetDeliveryMethod.UnreliableSequenced:
									if (nr < m_expectedSequenced[chan])
										m_sequencedErrors[chan]++;
									else
										m_sequencedCorrect[chan]++;
									m_expectedSequenced[chan] = nr + 1;
									break;
							}
							break;
					}
					Server.Recycle(msg);
				}
				Thread.Sleep(0);

				double now = NetTime.Now;
				if (now > m_lastLabelUpdate + kLabelUpdateFrequency)
				{
					UpdateLabel();
					m_lastLabelUpdate = now;
				}
			}
		}

		private static void UpdateLabel()
		{
			if (Server.ConnectionsCount < 1)
			{
				// don't update! Keep old...
				const string oldData = "(Note: OLD DATA - NO CONNECTIONS NOW)";
				if (!MainForm.label1.Text.EndsWith(oldData))
					MainForm.label1.Text += oldData;
			}
			else
			{
				StringBuilder bdr = new StringBuilder();
				bdr.Append(Server.Statistics.ToString());
				bdr.Append(Server.Connections[0].Statistics.ToString());
				bdr.AppendLine("RECEIVED Reliable ordered: " + 
					m_reliableOrderedCorrect[0] + ", " +
					m_reliableOrderedCorrect[1] + ", " +
					m_reliableOrderedCorrect[2] + 
					" received; " +
					m_reliableOrderedErrors[0] + ", " +
					m_reliableOrderedErrors[1] + ", " +
					m_reliableOrderedErrors[2] +
					" errors");
				bdr.AppendLine("RECEIVED Sequenced: " +
					m_sequencedCorrect[0] + ", " +
					m_sequencedCorrect[1] + ", " +
					m_sequencedCorrect[2] +
					" received; " +
					m_sequencedErrors[0] + ", " +
					m_sequencedErrors[1] + ", " +
					m_sequencedErrors[2] +
					" errors");
				MainForm.label1.Text = bdr.ToString();
			}
		}
	}
}
