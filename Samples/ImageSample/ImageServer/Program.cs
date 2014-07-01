using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Lidgren.Network;
using SamplesCommon;
using System.Drawing;

namespace ImageServer
{
	static class Program
	{
		public static Form1 MainForm;
		public static NetServer Server;
		public static byte[] ImageData;
		public static int ImageWidth, ImageHeight;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			// create a configuration, use identifier "ImageTransfer" - same as client
			NetPeerConfiguration config = new NetPeerConfiguration("ImageTransfer");
			config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
			config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			config.EnableMessageType(NetIncomingMessageType.DebugMessage);
			config.AutoExpandMTU = true;

			// listen on port 14242
			config.Port = 14242;

			Server = new NetServer(config);

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(MainForm);
		}

		static void AppLoop(object sender, EventArgs e)
		{
			NetIncomingMessage inc;

			while (NativeMethods.AppStillIdle)
			{
				// read any pending messages
				while ((inc = Server.WaitMessage(100)) != null)
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
						case NetIncomingMessageType.DiscoveryRequest:
							NetOutgoingMessage dom = Server.CreateMessage();
							dom.Write("Kokosboll");
							Server.SendDiscoveryResponse(dom, inc.SenderEndPoint);
							break;
						case NetIncomingMessageType.ConnectionApproval:

							// Here we could check inc.SenderConnection.RemoteEndPoint, deny certain ip

							// check hail data
							try
							{
								int a = inc.ReadInt32();
								string s = inc.ReadString();

								if (a == 42 && s == "secret")
									inc.SenderConnection.Approve();
								else
									inc.SenderConnection.Deny("Bad approve data, go away!");
							}
							catch (NetException)
							{
								inc.SenderConnection.Deny("Bad approve data, go away!");
							}
							break;
						case NetIncomingMessageType.StatusChanged:
							NetConnectionStatus status = (NetConnectionStatus)inc.ReadByte();
							NativeMethods.AppendText(MainForm.richTextBox1, "New status: " + status + " (" + inc.ReadString() + ")");
							if (status == NetConnectionStatus.Connected)
							{
								//
								// A client connected; send the entire image in chunks of 990 bytes
								//
								/*
								uint seg = 0;
								int ptr = 0;
								while (ptr < ImageData.Length)
								{
									int l = ImageData.Length - ptr > 990 ? 990 : ImageData.Length - ptr;
									NetOutgoingMessage om = Server.CreateMessage(l);
									om.Write((ushort)ImageWidth);
									om.Write((ushort)ImageHeight);
									om.WriteVariableUInt32(seg++);
									om.Write(ImageData, ptr, l);
									ptr += 990;

									Server.SendMessage(om, inc.SenderConnection, NetDeliveryMethod.ReliableUnordered, 0);
								}
								*/

								//
								// A client connected; send the entire image in one very large message that will be fragmented automatically by the library
								//
								NetOutgoingMessage om = Server.CreateMessage(ImageData.Length + 5);

								om.Write((ushort)ImageWidth);
								om.Write((ushort)ImageHeight);
								om.Write(ImageData);

								Server.SendMessage(om, inc.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

								// all messages will be sent before disconnect so we can call it here
								// inc.SenderConnection.Disconnect("Bye bye now");
							}

							if (status == NetConnectionStatus.Disconnected)
								NativeMethods.AppendText(MainForm.richTextBox1, inc.SenderConnection.Statistics.ToString());

							break;
					}

					// recycle message to avoid garbage
					Server.Recycle(inc);
				}
			}
		}

		public static void Start(string filename)
		{
			if (Server.Status != NetPeerStatus.NotRunning)
			{
				Server.Shutdown("Restarting");
				System.Threading.Thread.Sleep(100);
			}

			Server.Start();

			MainForm.Text = "Server: Running";

			// get image size
			Bitmap bm = Bitmap.FromFile(filename) as Bitmap;
			ImageWidth = bm.Width;
			ImageHeight = bm.Height;

			// extract color bytes
			// very slow method, but small code size
			ImageData = new byte[3 * ImageWidth * ImageHeight];
			int ptr = 0;
			for (int y = 0; y < ImageHeight; y++)
			{
				for (int x = 0; x < ImageWidth; x++)
				{
					Color color = bm.GetPixel(x, y);
					ImageData[ptr++] = color.R;
					ImageData[ptr++] = color.G;
					ImageData[ptr++] = color.B;
				}
			}

			bm.Dispose();
		}
	}
}
