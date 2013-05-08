using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Lidgren.Network;
using SamplesCommon;
using System.IO;

namespace FileStreamClient
{
	static class Program
	{
		private static Form1 s_form;
		private static NetClient s_client;
		private static ulong s_length;
		private static ulong s_received;
		private static FileStream s_writeStream;
		private static int s_timeStarted;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			s_form = new Form1();

			NetPeerConfiguration config = new NetPeerConfiguration("filestream");
			s_client = new NetClient(config);
			s_client.Start();

			Application.Idle += new EventHandler(AppLoop);
			Application.Run(s_form);

			s_client.Shutdown("Application exiting");
		}

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage inc;
				while ((inc = s_client.ReadMessage()) != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.Data:
							int chunkLen = inc.LengthBytes;
							if (s_length == 0)
							{
								s_length = inc.ReadUInt64();
								string filename = inc.ReadString();
								s_form.Text = "Starting...";
								s_writeStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
								s_timeStarted = Environment.TickCount;
								break;
							}

							byte[] all = inc.ReadBytes(inc.LengthBytes);
							s_received += (ulong)all.Length;
							s_writeStream.Write(all, 0, all.Length);

							int v = (int)(((float)s_received / (float)s_length) * 100.0f);
							if (s_form.progressBar1.Value != v)
							{
								s_form.progressBar1.Value = v;
								int passed = Environment.TickCount - s_timeStarted;
								double psec = (double)passed / 1000.0;
								double bps = (double)s_received / psec;

								s_form.Text = NetUtility.ToHumanReadable((long)bps) + " per second";
							}

							if (s_received >= s_length)
							{
								int passed = Environment.TickCount - s_timeStarted;
								double psec = (double)passed / 1000.0;
								double bps = (double)s_received / psec;
								s_form.Text = "Done at " + NetUtility.ToHumanReadable((long)bps) + " per second";
								s_form.progressBar1.Value = 100;

								s_writeStream.Flush();
								s_writeStream.Close();
								s_writeStream.Dispose();

								s_client.Disconnect("Everything received, bye!");
							}
							break;
					}
					s_client.Recycle(inc);
				}
			}
		}

		internal static void Connect(string host, int port)
		{
			s_length = 0;
			s_received = 0;
			s_client.Connect(host, port);
		}
	}
}
