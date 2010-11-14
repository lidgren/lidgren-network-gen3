using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChatClient
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();

			textBox1.KeyDown += new KeyEventHandler(textBox1_KeyDown);
		}

		public void EnableInput()
		{
			textBox1.Enabled = true;
			button1.Enabled = true;
		}

		public void DisableInput()
		{
			textBox1.Enabled = false;
			button1.Enabled = false;
		}

		void textBox1_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				// return is equivalent to clicking "send"
				button1_Click(sender, e);
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			Program.DisplaySettings();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (button2.Text == "Connect")
			{
				int port;
				Int32.TryParse(textBox3.Text, out port);
				Program.Connect(textBox2.Text, port);
				button2.Text = "Disconnect";
			}
			else
			{
				Program.Shutdown();
				button2.Text = "Connect";
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			// Send
			if (!string.IsNullOrEmpty(textBox1.Text))
				Program.Send(textBox1.Text);
			textBox1.Text = "";

		}
	}
}
