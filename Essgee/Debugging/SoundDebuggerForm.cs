using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Essgee.Sound;

namespace Essgee.Debugging
{
	public partial class SoundDebuggerForm : Form, IDebuggerForm
	{
		public WaveformControl WaveformControl => waveformControl;

		public SoundDebuggerForm()
		{
			InitializeComponent();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = true;

			Program.Configuration.DebugWindows[GetType().Name] = Location;

			Hide();
		}
	}
}
