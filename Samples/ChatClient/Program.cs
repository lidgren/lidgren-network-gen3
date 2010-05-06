using System;
using System.Threading;
using System.Windows.Forms;

using Lidgren.Network;

using SamplesCommon;

namespace ChatClient
{
	public class ChatMessage
	{
		public string Sender;
		public string Text;
	}

	static class Program
	{
		public static Form1 MainForm;
		public static NetClient Client;
		public static NetPeerSettingsWindow SettingsWindow;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			Client = new NetClient(new NetPeerConfiguration("Chat"));
			Client.Start();

			Display("Type 'connect <host>' to connect to a server");

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);
		}

		private static void Display(string text)
		{
			NativeMethods.AppendText(MainForm.richTextBox1, text);
		}

		public static void Input(string input)
		{
			if (input.ToLowerInvariant().StartsWith("connect "))
			{
				string host = input.Substring(8).Trim();
				if (string.IsNullOrEmpty(host))
					host = "localhost";

				Client.Connect(host, 14242);
				return;
			}

			// send chat message
			ChatMessage cm = new ChatMessage();
			cm.Sender = Client.UniqueIdentifier.ToString();
			cm.Text = input;

			NetOutgoingMessage om = Client.CreateMessage();
			om.WriteAllFields(cm);
			Client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
		}

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage msg = Client.ReadMessage();
				if (msg != null)
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.ErrorMessage:
						case NetIncomingMessageType.WarningMessage:
							// print any diagnostics message
							Display(msg.ReadString());
							break;

						case NetIncomingMessageType.StatusChanged:
							// print changes in connection(s) status
							NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();
							string reason = msg.ReadString();
							Display("Status: " + status + " (" + reason + ")");

							break;

						case NetIncomingMessageType.Data:

							ChatMessage cm = new ChatMessage();
							msg.ReadAllFields(cm);

							Display("Received from " + cm.Sender + ": " + cm.Text);
							break;
					}
					Client.Recycle(msg);
				}
				Thread.Sleep(1);
			}
		}
	}
}
