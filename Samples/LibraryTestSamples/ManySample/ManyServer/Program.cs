using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Lidgren.Network;
using SamplesCommon;

namespace ManyServer
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

			NetPeerConfiguration config = new NetPeerConfiguration("many");
			config.Port = 14242;
#if DEBUG
			config.SimulatedLoss = 0.02f;
#else
			// throw new Exception("Sample not relevant in RELEASE; statistics needed to make sense!");
#endif
			config.MaximumConnections = 256;

			Server = new NetServer(config);
			Server.Start();

			var swin = new NetPeerSettingsWindow("Server settings", Program.Server);
			swin.Show();

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);
		}

		static void AppLoop(object sender, EventArgs e)
		{
			NetIncomingMessage inc;

			while (NativeMethods.AppStillIdle)
			{
				// read any pending messages
				while ((inc = Server.ReadMessage()) != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							// just print any message
							string str = inc.ReadString();
							NativeMethods.AppendText(MainForm.richTextBox1, str);
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
							string reason = inc.ReadString();
							NativeMethods.AppendText(MainForm.richTextBox1, NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + ": " + reason);
							MainForm.Text = Server.ConnectionsCount + " connections";
							break;
						case NetIncomingMessageType.Data:
							string dstr = "Data from " + NetUtility.ToHexString(inc.SenderConnection.RemoteUniqueIdentifier) + ": " + inc.ReadString();
							//NativeMethods.AppendText(MainForm.richTextBox1, dstr);

							NetOutgoingMessage outMsg = Server.CreateMessage();
							outMsg.Write(dstr);

							var conns = Server.Connections;

							// resend to ONE random connection
							//Server.SendMessage(outMsg, conns[NetRandom.Instance.Next(0, conns.Count)], NetDeliveryMethod.ReliableOrdered, 0);

							List<NetConnection> rec = new List<NetConnection>();
							rec.AddRange(conns);
							rec.Remove(inc.SenderConnection);
							if (rec.Count > 0)
								Server.SendMessage(outMsg, rec, NetDeliveryMethod.ReliableOrdered, 0);
							break;
					}
				}

				System.Threading.Thread.Sleep(1);
			}
		}
	}
}
