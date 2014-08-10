using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SamplesCommon;
using Lidgren.Network;
using System.Threading;

namespace SpeedTestClient
{
	static class Program
	{
		private static NetClient s_client;
		private static Form1 s_form;
		private static NetPeerSettingsWindow s_settingsWindow;

		private static NetDeliveryMethod s_method;
		private static int s_sequenceChannel;

		private static long s_sentBytes;
		private static float s_lastUpdatedTitle;
		private const float s_titleUpdateInterval = 0.75f;
		private static long[] s_nextSendNumber = new long[256];

		private const float s_sendInterval = 0.1f;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			s_form = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("speedtest");
			config.AutoExpandMTU = true;
			s_client = new NetClient(config);

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
							string reason = im.ReadString();
							Output(status.ToString() + ": " + reason);
							s_form.Text = "Speed test client: " + status.ToString();
							break;
						case NetIncomingMessageType.Data:
							//string chat = im.ReadString();
							//Output(chat);
							break;
						default:
							Output("Unhandled type: " + im.MessageType);
							break;
					}
					s_client.Recycle(im);
				}

				float now = (float)NetTime.Now;
				if (now > s_lastUpdatedTitle + s_titleUpdateInterval)
				{
					s_form.Text = "Speed test client: " + (s_client.ServerConnection != null ? s_client.ServerConnection.Status.ToString() : "(no connection)") + " " + NetUtility.ToHumanReadable(s_sentBytes) + " bytes sent";
					s_lastUpdatedTitle = now;
				}

				if (s_client.ServerConnection != null && s_client.ServerConnection.Status == NetConnectionStatus.Connected)
				{
					//
					// Saturate the line
					//
					int windowSize, freeWindowSlots;
					s_client.ServerConnection.GetSendQueueInfo(s_method, s_sequenceChannel, out windowSize, out freeWindowSlots);

					// queue up to double window size
					if (windowSize == 0)
						freeWindowSlots = 1;

					int num = 0;
					while(freeWindowSlots > -windowSize)
					{
						// send random data
						int size = s_client.Configuration.MaximumTransmissionUnit - 30;
						NetOutgoingMessage om = s_client.CreateMessage(size);
						byte[] tmp = new byte[size];
						MWCRandom.Instance.NextBytes(tmp);
						int slot = (int)s_method + s_sequenceChannel;
						om.Write(s_nextSendNumber[slot]);
						s_nextSendNumber[slot]++;
						om.Write(tmp);

						// queue message for sending
						NetSendResult res = s_client.SendMessage(om, s_method, s_sequenceChannel);
						if (s_method != NetDeliveryMethod.Unreliable && s_method != NetDeliveryMethod.UnreliableSequenced)
						{
							if (res != NetSendResult.Queued && res != NetSendResult.Sent)
								throw new NetException("Got res " + res);
						}
						s_sentBytes += size;

						freeWindowSlots--;
						num++;
					}
					//Console.WriteLine("Queued " + num + " messages");

					//
					// Send every X millisecond
					//
					/*
					if (now > s_lastSend + s_sendInterval)
					{

						// send random data
						int size = s_client.Configuration.MaximumTransmissionUnit - 25;
						NetOutgoingMessage om = s_client.CreateMessage(size);
						byte[] tmp = new byte[size];
						NetRandom.Instance.NextBytes(tmp);
						int slot = (int)s_method + s_sequenceChannel;
						om.Write(s_nextSendNumber[slot]);
						s_nextSendNumber[slot]++;
						om.Write(tmp);

						// queue message for sending
						NetSendResult res = s_client.SendMessage(om, s_method, s_sequenceChannel);
						if (res != NetSendResult.Queued && res != NetSendResult.Sent)
							throw new NetException("Got res " + res);
						//Console.WriteLine("Res: " + res);
						s_sentBytes += size;

						s_lastSend = now;
					}
					*/
				}

				Thread.Sleep(0);
			}
		}
		
		// called by the UI
		internal static void Connect(string host, int port, string deliveryMethod, int sequenceChannel)
		{
			s_client.Start();

			s_method = (NetDeliveryMethod)Enum.Parse(typeof(NetDeliveryMethod), deliveryMethod);
			s_sequenceChannel = sequenceChannel;

			s_client.Connect(host, port);
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
					s_settingsWindow = new NetPeerSettingsWindow("Speed test client settings", s_client);
				s_settingsWindow.Show();
			}
		}
	}
}
