using System;
using System.Collections.Generic;
using System.Windows.Forms;

using SamplesCommon;

using Lidgren.Network;

namespace UnconnectedSample
{
	static class Program
	{
		public static Form1 MainForm;
		public static NetPeer Peer;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("unconntest");
			config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			config.AcceptIncomingConnections = false; // don't accept connections; we're just using unconnected messages in this sample

			Peer = new NetPeer(config);
			Peer.Start();

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);
		}

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				// read any incoming messages
				NetIncomingMessage im;
				while((im = Peer.ReadMessage()) != null)
				{
					switch (im.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							MainForm.richTextBox1.AppendText(im.ReadString() + Environment.NewLine);
							break;
						case NetIncomingMessageType.UnconnectedData:
							MainForm.richTextBox1.AppendText("Received from " + im.SenderEndPoint + ": " + im.ReadString() + Environment.NewLine);
							break;
					}
					Peer.Recycle(im);
				}

				System.Threading.Thread.Sleep(1);
			}
		}

		internal static void Send(string host, int port, string text)
		{
			NetOutgoingMessage om = Peer.CreateMessage();
			om.Write(text);

			Peer.SendUnconnectedMessage(om, host, port);
		}
	}
}
