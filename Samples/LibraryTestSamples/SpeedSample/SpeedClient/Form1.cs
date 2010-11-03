using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SpeedTestClient
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			comboBox1.SelectedIndex = 0;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			int seqChan;
			Int32.TryParse(textBox3.Text, out seqChan);
			int port;
			Int32.TryParse(textBox2.Text, out port);
			Program.Connect(textBox1.Text, port, comboBox1.SelectedItem.ToString(), seqChan);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Program.DisplaySettings();
		}
	}
}
