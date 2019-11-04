using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Essgee.EventArguments;
using Essgee.Extensions;

namespace Essgee.Debugging
{
	public partial class SoundDebuggerForm : Form, IDebuggerForm
	{
		Timer refreshTimer;

		public SoundDebuggerForm()
		{
			InitializeComponent();

			refreshTimer = new Timer
			{
				Interval = (int)(1000 / 60.0),
				Enabled = true
			};
			refreshTimer.Tick += ((s, e) =>
			{
				waveformControlCh1.Invalidate();
				waveformControlCh2.Invalidate();
				waveformControlCh3.Invalidate();
				waveformControlCh4.Invalidate();
				waveformControlChAll.Invalidate();
			});
			refreshTimer.Start();
		}

		public void EnqueueSamples(object sender, EnqueueSamplesEventArgs e)
		{
			waveformControlCh1.EnqueueSamples(e.ChannelSamples[0]);
			waveformControlCh2.EnqueueSamples(e.ChannelSamples[1]);
			waveformControlCh3.EnqueueSamples(e.ChannelSamples[2]);
			waveformControlCh4.EnqueueSamples(e.ChannelSamples[3]);

			this.CheckInvokeMethod(() =>
			{
				lblChannel1Muted.Text = (e.IsChannelMuted[0] ? "(Muted)" : string.Empty);
				lblChannel2Muted.Text = (e.IsChannelMuted[1] ? "(Muted)" : string.Empty);
				lblChannel3Muted.Text = (e.IsChannelMuted[2] ? "(Muted)" : string.Empty);
				lblChannel4Muted.Text = (e.IsChannelMuted[3] ? "(Muted)" : string.Empty);
			});

			waveformControlChAll.EnqueueSamples(e.MixedSamples);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = true;

			Program.Configuration.DebugWindows[GetType().Name] = Location;

			Hide();
		}
	}
}
