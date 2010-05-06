using System;
using System.Threading;
using System.Windows.Forms;

using Lidgren.Network;

using SamplesCommon;

namespace ChatServer
{
	public class ChatMessage
	{
		public string Sender { get; set; }
		public string Text { get; set; }
	}

	static class Program
	{
		public static Form1 MainForm;
		public static NetServer Server;
		public static NetPeerSettingsWindow SettingsWindow;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			// create a configuration
			NetPeerConfiguration config = new NetPeerConfiguration("Chat");
			config.Port = 14242;

			// create and start server
			Server = new NetServer(config);
			Server.Start();

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);
		}

		private static void Display(string text)
		{
			NativeMethods.AppendText(MainForm.richTextBox1, text);
		}

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage msg = Server.WaitMessage(100);
				if (msg != null)
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.ErrorMessage:
						case NetIncomingMessageType.WarningMessage:
							// print any library message
							Display(msg.ReadString());
							break;

						case NetIncomingMessageType.StatusChanged:
							// print changes in connection(s) status
							NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
							string reason = msg.ReadString();
							Display(msg.SenderConnection + " status: " + status + " (" + reason + ")");

							break;

						case NetIncomingMessageType.Data:

							// read chat message
							ChatMessage cm = new ChatMessage();
							msg.ReadAllProperties(cm);

							// Forward all data to all clients (including sender for debugging purposes)
							NetOutgoingMessage om = Server.CreateMessage();
							om.WriteAllProperties(cm, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

							Display("Forwarding text from " + cm.Sender + " to all clients: " + cm.Text);
							Server.SendMessage(om, Server.Connections, NetDeliveryMethod.ReliableUnordered, 0);

							break;
					}
					Server.Recycle(msg);
				}
			}
		}
	}
}
