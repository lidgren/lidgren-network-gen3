using System;
using System.Net;
using System.Collections.Generic;
using System.Windows.Forms;

using Lidgren.Network;

using MSCommon;

namespace MSClient
{
	static class Program
	{
		private static Form1 m_mainForm;
		private static NetClient m_client;
		private static IPEndPoint m_masterServer;
		private static List<IPEndPoint[]> m_hostList;

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			m_mainForm = new Form1();

			m_hostList = new List<IPEndPoint[]>();

			NetPeerConfiguration config = new NetPeerConfiguration("game");
			config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			config.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
			m_client = new NetClient(config);
			m_client.Start();

			Application.Idle += new EventHandler(AppIdle);
			Application.Run(m_mainForm);
		}

		static void AppIdle(object sender, EventArgs e)
		{
			while (NativeMethods.AppStillIdle)
			{
				NetIncomingMessage inc;
				while ((inc = m_client.ReadMessage()) != null)
				{
					switch (inc.MessageType)
					{
						case NetIncomingMessageType.VerboseDebugMessage:
						case NetIncomingMessageType.DebugMessage:
						case NetIncomingMessageType.WarningMessage:
						case NetIncomingMessageType.ErrorMessage:
							NativeMethods.AppendText(m_mainForm.richTextBox1, inc.ReadString());
							break;
						case NetIncomingMessageType.UnconnectedData:
							if (inc.SenderEndPoint.Equals(m_masterServer))
							{
								// it's from the master server - must be a host
								IPEndPoint hostInternal = inc.ReadIPEndPoint();
								IPEndPoint hostExternal = inc.ReadIPEndPoint();
		
								m_hostList.Add(new IPEndPoint[] { hostInternal, hostExternal });

								m_mainForm.comboBox1.Items.Add(hostExternal.Address.ToString());
							}
							break;
						case NetIncomingMessageType.NatIntroductionSuccess:
							string token = inc.ReadString();
							MessageBox.Show("Nat introduction success to " + inc.SenderEndPoint + " token is: " + token);
							break;
					}
				}
			}
		}

		public static void GetServerList(string masterServerAddress)
		{
			//
			// Send request for server list to master server
			//
			m_masterServer = new IPEndPoint(NetUtility.Resolve(masterServerAddress), CommonConstants.MasterServerPort);

			NetOutgoingMessage listRequest = m_client.CreateMessage();
			listRequest.Write((byte)MasterServerMessageType.RequestHostList);
			m_client.SendUnconnectedMessage(listRequest, m_masterServer);
		}

		public static void RequestNATIntroduction(string host)
		{
			if (string.IsNullOrEmpty(host))
			{
				MessageBox.Show("Select a host in the list first");
				return;
			}

			if (m_masterServer == null)
				throw new Exception("Must connect to master server first!");

			NetOutgoingMessage om = m_client.CreateMessage();
			om.Write((byte)MasterServerMessageType.RequestIntroduction);

			// write internal ipendpoint
			IPAddress mask;
			om.Write(new IPEndPoint(NetUtility.GetMyAddress(out mask), m_client.Port));

			// write external address of host to request introduction to
			IPEndPoint hostEp = new IPEndPoint(NetUtility.Resolve(host), CommonConstants.GameServerPort);
			om.Write(hostEp);
			om.Write("mytoken");

			m_client.SendUnconnectedMessage(om, m_masterServer);
		}
	}
}
