using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MSCommon
{
	public static class NativeMethods
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
		public const int WM_VSCROLL = 277; // Vertical scroll
		public const int SB_BOTTOM = 7; // Scroll to bottom 

		[StructLayout(LayoutKind.Sequential)]
		public struct PeekMsg
		{
			public IntPtr hWnd;
			public Message msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public System.Drawing.Point p;
		}

		[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
		[DllImport("User32.dll", CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(out PeekMsg msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

		public static bool AppStillIdle
		{
			get
			{
				PeekMsg msg;
				return !PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
			}
		}

		public static void AppendText(RichTextBox box, string line)
		{
			try
			{
				box.AppendText(line + Environment.NewLine);
				ScrollRichTextBox(box);
			}
			catch
			{
			}
		}

		public static void ScrollRichTextBox(RichTextBox box)
		{
			if (box == null || box.IsDisposed || box.Disposing)
				return;
			SendMessage(box.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
		}
	}
}
