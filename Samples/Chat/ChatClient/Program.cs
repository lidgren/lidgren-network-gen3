using System;
using System.Threading;
using System.Windows.Forms;

using Lidgren.Network;

using SamplesCommon;

namespace ChatClient
{
	static class Program
	{
		private static NetClient s_client;
		private static Form1 s_form;
		private static NetPeerSettingsWindow s_settingsWindow;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			s_form = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("chat");
			config.AutoFlushSendQueue = false;
			s_client = new NetClient(config);

			s_client.RegisterReceivedCallback(new SendOrPostCallback(GotMessage)); 

			Application.Run(s_form);

			s_client.Shutdown("Bye");
		}

		private static void Output(string text)
		{
			NativeMethods.AppendText(s_form.richTextBox1, text);
		}

		public static void GotMessage(object peer)
		{
			NetIncomingMessage im;
			while ((im = s_client.ReadMessage()) != null)
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

						if (status == NetConnectionStatus.Connected)
							s_form.EnableInput();
						else
							s_form.DisableInput();

						if (status == NetConnectionStatus.Disconnected)
							s_form.button2.Text = "Connect";

						string reason = im.ReadString();
						Output(status.ToString() + ": " + reason);

						break;
					case NetIncomingMessageType.Data:
						string chat = im.ReadString();
						Output(chat);
						break;
					default:
						Output("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes");
						break;
				}
				s_client.Recycle(im);
			}
		}

		// called by the UI
		public static void Connect(string host, int port)
		{
			s_client.Start();
			NetOutgoingMessage hail = s_client.CreateMessage("This is the hail message");
			s_client.Connect(host, port, hail);
		}

		// called by the UI
		public static void Shutdown()
		{
			s_client.Disconnect("Requested by user");
			// s_client.Shutdown("Requested by user");
		}

		// called by the UI
		public static void Send(string text)
		{
			NetOutgoingMessage om = s_client.CreateMessage(text);
			s_client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
			Output("Sending '" + text + "'");
			s_client.FlushSendQueue();
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
					s_settingsWindow = new NetPeerSettingsWindow("Chat client settings", s_client);
				s_settingsWindow.Show();
			}
		}
	}
}
