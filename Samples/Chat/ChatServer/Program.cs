using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

using Lidgren.Network;

using SamplesCommon;

namespace ChatServer
{
	static class Program
	{
		private static Form1 s_form;
		private static NetServer s_server;
		private static NetPeerSettingsWindow s_settingsWindow;
		
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			s_form = new Form1();

			// set up network
			NetPeerConfiguration config = new NetPeerConfiguration("chat");
			config.MaximumConnections = 100;
			config.Port = 14242;
			s_server = new NetServer(config);

			Application.Idle += new EventHandler(Application_Idle);
			Application.Run(s_form);
		}

		private static void Output(string text)
		{
			NativeMethods.AppendText(s_form.richTextBox1, text);
		}

		private static void Application_Idle(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage im;
				while ((im = s_server.ReadMessage()) != null)
				{
					// handle incoming message
					switch (im.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.ErrorMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
							string text = im.ReadString();
							Output(text);
							break;

						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

							string reason = im.ReadString();
							Output(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

							if (status == NetConnectionStatus.Connected)
								Output("Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());

							UpdateConnectionsList();
							break;
						case NetIncomingMessageType.Data:
							// incoming chat message from a client
							string chat = im.ReadString();

							Output("Broadcasting '" + chat + "'");

							// broadcast this to all connections, except sender
							List<NetConnection> all = s_server.Connections; // get copy
							all.Remove(im.SenderConnection);

							if (all.Count > 0)
							{
								NetOutgoingMessage om = s_server.CreateMessage();
								om.Write(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " said: " + chat);
								s_server.SendMessage(om, all, NetDeliveryMethod.ReliableOrdered, 0);
							}
							break;
						default:
							Output("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes " + im.DeliveryMethod + "|" + im.SequenceChannel);
							break;
					}
					s_server.Recycle(im);
				}
				Thread.Sleep(1);
			}
		}

		private static void UpdateConnectionsList()
		{
			s_form.listBox1.Items.Clear();

			foreach (NetConnection conn in s_server.Connections)
			{
				string str = NetUtility.ToHexString(conn.RemoteUniqueIdentifier) + " from " + conn.RemoteEndPoint.ToString() + " [" + conn.Status + "]";
				s_form.listBox1.Items.Add(str);
			}
		}

		// called by the UI
		public static void StartServer()
		{
			s_server.Start();
		}

		// called by the UI
		public static void Shutdown()
		{
			s_server.Shutdown("Requested by user");
		}

		// called by the UI
		public static void DisplaySettings()
		{
			if (s_settingsWindow != null && s_settingsWindow.Visible)
			{
				s_settingsWindow.Hide();
			}
			else
			{
				if (s_settingsWindow == null || s_settingsWindow.IsDisposed)
					s_settingsWindow = new NetPeerSettingsWindow("Chat server settings", s_server);
				s_settingsWindow.Show();
			}
		}
	}
}
