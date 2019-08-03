﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Essgee.EventArguments
{
	public class PollInputEventArgs : EventArgs
	{
		public IEnumerable<Keys> Keyboard { get; set; }
		public MouseButtons MouseButtons { get; set; }
		public (int X, int Y) MousePosition { get; set; }

		public PollInputEventArgs()
		{
			Keyboard = new List<Keys>();
			MouseButtons = MouseButtons.None;
			MousePosition = (0, 0);
		}
	}
}
