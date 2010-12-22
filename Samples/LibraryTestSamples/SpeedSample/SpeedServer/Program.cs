using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Lidgren.Network;
using SamplesCommon;
using System.Threading;
using System.Text;

namespace SpeedTestServer
{
	static class Program
	{
		private static Form1 s_form;
		private static NetServer s_server;
		private static NetPeerSettingsWindow s_settingsWindow;

		private const double s_labelUpdateInterval = 0.5f;
		private static double s_lastLabelUpdate;

		private static long s_totalBytesReceived;
		private static long s_bpsBytes;
		private static long[] s_nextNumber = new long[256];

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			s_form = new Form1();

			// set up network
			NetPeerConfiguration config = new NetPeerConfiguration("speedtest");
			config.MaximumConnections = 1;
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
							break;
						case NetIncomingMessageType.Data:

							long nr = im.ReadInt64();
							int slot = (int)im.DeliveryMethod + im.SequenceChannel;
							long expected = s_nextNumber[slot];
							switch (im.DeliveryMethod)
							{
								case NetDeliveryMethod.Unreliable:
									// anything is FINE
									break;
								case NetDeliveryMethod.UnreliableSequenced:
								case NetDeliveryMethod.ReliableSequenced:
									if (nr < expected)
										throw new NetException(im.DeliveryMethod.ToString() + " failed! Expected " + expected + " received " + nr);
									s_nextNumber[slot] = nr + 1;
									break;
								case NetDeliveryMethod.ReliableUnordered:
									// anything is fine, basically
									break;
								case NetDeliveryMethod.ReliableOrdered:
									if (expected != nr)
										throw new NetException(im.DeliveryMethod.ToString() + " failed! Expected " + expected + " received " + nr);
									s_nextNumber[slot] = nr + 1;
									break;
							}

							int len = im.LengthBytes;
							s_totalBytesReceived += len;
							s_bpsBytes += len;
							break;
						default:
							Output("Unhandled type: " + im.MessageType);
							break;
					}
					s_server.Recycle(im);
				}

				double now = NetTime.Now;
				if (now > s_lastLabelUpdate + s_labelUpdateInterval)
				{
					//
					// Update label
					//

					StringBuilder bdr = new StringBuilder();
					bdr.Append(s_server.Statistics.ToString());

					List<NetConnection> conns = s_server.Connections;
					if (conns.Count > 0)
						bdr.Append(conns[0].Statistics.ToString());

					bdr.AppendLine("Total bytes received: " + NetUtility.ToHumanReadable(s_totalBytesReceived));
					
					// calculate bytes per second
					double time = now - s_lastLabelUpdate;
					double bps = (double)s_bpsBytes / time;
					s_bpsBytes = 0;

					bdr.AppendLine("Bytes/second: " + NetUtility.ToHumanReadable((long)bps));

					s_form.label1.Text = bdr.ToString();

					s_lastLabelUpdate = now;
				}

				Thread.Sleep(0);
			}
		}

		// called by the UI
		public static void Start()
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
					s_settingsWindow = new NetPeerSettingsWindow("Speed test server settings", s_server);
				s_settingsWindow.Show();
			}
		}
	}
}
