using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MSClient
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			Program.GetServerList(textBox1.Text);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (comboBox1.SelectedItem == null)
				return;

			var splits = comboBox1.SelectedItem.ToString().Split(' ');
			var host = Int64.Parse(splits[0]);
			Program.RequestNATIntroduction(host);
		}
	}
}
