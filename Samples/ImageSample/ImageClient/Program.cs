using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SamplesCommon;
using Lidgren.Network;

namespace ImageClient
{
	static class Program
	{
		public static Form1 MainForm;
		public static List<ImageGetter> Getters = new List<ImageGetter>();

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			MainForm = new Form1();

			try
			{
				Application.Idle += new EventHandler(AppLoop);
				Application.Run(MainForm);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ouch: " + ex);
			}
		}

		static void AppLoop(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				foreach (ImageGetter getter in Getters)
					getter.Heartbeat();
				System.Threading.Thread.Sleep(1);
			}
		}

		internal static void SpawnGetter(string host, NetPeerConfiguration copyConfig)
		{
			ImageGetter getter = new ImageGetter(host, copyConfig);
			Getters.Add(getter);
			getter.Show();
		}
	}
}
