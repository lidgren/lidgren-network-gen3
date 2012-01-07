using System;
using System.Windows.Forms;

namespace UnconnectedSample
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			uint port;
			if (UInt32.TryParse(textBox2.Text, out port) == false)
			{
				MessageBox.Show("Please fill in the port of the target peer");
				return;
			}

			Program.Send(textBox1.Text, (int)port, textBox3.Text);
		}
	}
}
