using System;
using System.Windows.Forms;

namespace FileStreamServer
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.CheckFileExists = true;
			var res = dlg.ShowDialog();

			if (res != DialogResult.OK)
				return;

			Program.Start(dlg.FileName);
		}
	}
}
