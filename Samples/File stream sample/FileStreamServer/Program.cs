using System;
using System.Windows.Forms;

using Lidgren.Network;
using SamplesCommon;

namespace FileStreamServer
{
	static class Program
	{
		private static Form1 s_form;
		private static NetServer s_server;
		private static string s_fileName;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			s_form = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("filestream");
			config.Port = 14242;
			s_server = new NetServer(config);

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(s_form);

			s_server.Shutdown("Application exiting");
		}

		public static void Output(string str)
		{
			if (s_form != null && s_form.richTextBox1 != null)
				NativeMethods.AppendText(s_form.richTextBox1, str);
		}

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage inc;
				while((inc = s_server.ReadMessage()) != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
						case NetIncomingMessageType.VerboseDebugMessage:
							Output(inc.ReadString());
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
							switch (status)
							{
								case NetConnectionStatus.Connected:
									// start streaming to this client
									inc.SenderConnection.Tag = new StreamingClient(inc.SenderConnection, s_fileName);
									Output("Starting streaming to " + inc.SenderConnection);
									break;
								default:
									Output(inc.SenderConnection + ": " + status + " (" + inc.ReadString() + ")");
									break;
							}
							break;
					}
					s_server.Recycle(inc);
				}

				// stream to all connections
				foreach (NetConnection conn in s_server.Connections)
				{
					StreamingClient client = conn.Tag as StreamingClient;
					if (client != null)
						client.Heartbeat();
				}

				System.Threading.Thread.Sleep(0);
			}
		}

		internal static void Start(string filename)
		{
			s_fileName = filename;
			s_server.Start();
		}
	}
}
