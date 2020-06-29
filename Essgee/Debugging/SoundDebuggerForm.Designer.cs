namespace Essgee.Debugging
{
	partial class SoundDebuggerForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tlpWaveforms = new System.Windows.Forms.TableLayoutPanel();
			this.lblChannel4Muted = new System.Windows.Forms.Label();
			this.lblChannel3Muted = new System.Windows.Forms.Label();
			this.lblChannel2Muted = new System.Windows.Forms.Label();
			this.lblChannel1Muted = new System.Windows.Forms.Label();
			this.lblChannel1 = new System.Windows.Forms.Label();
			this.lblChannel2 = new System.Windows.Forms.Label();
			this.lblChannel3 = new System.Windows.Forms.Label();
			this.lblChannel4 = new System.Windows.Forms.Label();
			this.lblChannelAll = new System.Windows.Forms.Label();
			this.waveformControlChAll = new Essgee.Sound.WaveformControl();
			this.waveformControlCh4 = new Essgee.Sound.WaveformControl();
			this.waveformControlCh1 = new Essgee.Sound.WaveformControl();
			this.waveformControlCh2 = new Essgee.Sound.WaveformControl();
			this.waveformControlCh3 = new Essgee.Sound.WaveformControl();
			this.tlpWaveforms.SuspendLayout();
			this.SuspendLayout();
			// 
			// tlpWaveforms
			// 
			this.tlpWaveforms.ColumnCount = 2;
			this.tlpWaveforms.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
			this.tlpWaveforms.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpWaveforms.Controls.Add(this.lblChannel4Muted, 0, 7);
			this.tlpWaveforms.Controls.Add(this.lblChannel3Muted, 0, 5);
			this.tlpWaveforms.Controls.Add(this.lblChannel2Muted, 0, 3);
			this.tlpWaveforms.Controls.Add(this.lblChannel1Muted, 0, 1);
			this.tlpWaveforms.Controls.Add(this.waveformControlChAll, 1, 8);
			this.tlpWaveforms.Controls.Add(this.waveformControlCh4, 1, 6);
			this.tlpWaveforms.Controls.Add(this.lblChannel1, 0, 0);
			this.tlpWaveforms.Controls.Add(this.lblChannel2, 0, 2);
			this.tlpWaveforms.Controls.Add(this.waveformControlCh1, 1, 0);
			this.tlpWaveforms.Controls.Add(this.waveformControlCh2, 1, 2);
			this.tlpWaveforms.Controls.Add(this.waveformControlCh3, 1, 4);
			this.tlpWaveforms.Controls.Add(this.lblChannel3, 0, 4);
			this.tlpWaveforms.Controls.Add(this.lblChannel4, 0, 6);
			this.tlpWaveforms.Controls.Add(this.lblChannelAll, 0, 8);
			this.tlpWaveforms.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tlpWaveforms.Location = new System.Drawing.Point(0, 0);
			this.tlpWaveforms.Name = "tlpWaveforms";
			this.tlpWaveforms.Padding = new System.Windows.Forms.Padding(3);
			this.tlpWaveforms.RowCount = 11;
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
			this.tlpWaveforms.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0F));
			this.tlpWaveforms.Size = new System.Drawing.Size(834, 616);
			this.tlpWaveforms.TabIndex = 6;
			// 
			// lblChannel4Muted
			// 
			this.lblChannel4Muted.AutoSize = true;
			this.lblChannel4Muted.Location = new System.Drawing.Point(6, 389);
			this.lblChannel4Muted.Name = "lblChannel4Muted";
			this.lblChannel4Muted.Size = new System.Drawing.Size(13, 13);
			this.lblChannel4Muted.TabIndex = 18;
			this.lblChannel4Muted.Text = "--";
			// 
			// lblChannel3Muted
			// 
			this.lblChannel3Muted.AutoSize = true;
			this.lblChannel3Muted.Location = new System.Drawing.Point(6, 267);
			this.lblChannel3Muted.Name = "lblChannel3Muted";
			this.lblChannel3Muted.Size = new System.Drawing.Size(13, 13);
			this.lblChannel3Muted.TabIndex = 17;
			this.lblChannel3Muted.Text = "--";
			// 
			// lblChannel2Muted
			// 
			this.lblChannel2Muted.AutoSize = true;
			this.lblChannel2Muted.Location = new System.Drawing.Point(6, 145);
			this.lblChannel2Muted.Name = "lblChannel2Muted";
			this.lblChannel2Muted.Size = new System.Drawing.Size(13, 13);
			this.lblChannel2Muted.TabIndex = 16;
			this.lblChannel2Muted.Text = "--";
			// 
			// lblChannel1Muted
			// 
			this.lblChannel1Muted.AutoSize = true;
			this.lblChannel1Muted.Location = new System.Drawing.Point(6, 23);
			this.lblChannel1Muted.Name = "lblChannel1Muted";
			this.lblChannel1Muted.Size = new System.Drawing.Size(13, 13);
			this.lblChannel1Muted.TabIndex = 7;
			this.lblChannel1Muted.Text = "--";
			// 
			// lblChannel1
			// 
			this.lblChannel1.AutoSize = true;
			this.lblChannel1.Location = new System.Drawing.Point(6, 3);
			this.lblChannel1.Name = "lblChannel1";
			this.lblChannel1.Size = new System.Drawing.Size(55, 13);
			this.lblChannel1.TabIndex = 6;
			this.lblChannel1.Text = "Channel 1";
			// 
			// lblChannel2
			// 
			this.lblChannel2.AutoSize = true;
			this.lblChannel2.Location = new System.Drawing.Point(6, 125);
			this.lblChannel2.Name = "lblChannel2";
			this.lblChannel2.Size = new System.Drawing.Size(55, 13);
			this.lblChannel2.TabIndex = 7;
			this.lblChannel2.Text = "Channel 2";
			// 
			// lblChannel3
			// 
			this.lblChannel3.AutoSize = true;
			this.lblChannel3.Location = new System.Drawing.Point(6, 247);
			this.lblChannel3.Name = "lblChannel3";
			this.lblChannel3.Size = new System.Drawing.Size(55, 13);
			this.lblChannel3.TabIndex = 11;
			this.lblChannel3.Text = "Channel 3";
			// 
			// lblChannel4
			// 
			this.lblChannel4.AutoSize = true;
			this.lblChannel4.Location = new System.Drawing.Point(6, 369);
			this.lblChannel4.Name = "lblChannel4";
			this.lblChannel4.Size = new System.Drawing.Size(55, 13);
			this.lblChannel4.TabIndex = 12;
			this.lblChannel4.Text = "Channel 4";
			// 
			// lblChannelAll
			// 
			this.lblChannelAll.AutoSize = true;
			this.lblChannelAll.Location = new System.Drawing.Point(6, 491);
			this.lblChannelAll.Name = "lblChannelAll";
			this.lblChannelAll.Size = new System.Drawing.Size(65, 13);
			this.lblChannelAll.TabIndex = 15;
			this.lblChannelAll.Text = "All Channels";
			// 
			// waveformControlChAll
			// 
			this.waveformControlChAll.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.waveformControlChAll.BackColor = System.Drawing.Color.Black;
			this.waveformControlChAll.ForeColor = System.Drawing.Color.LightSteelBlue;
			this.waveformControlChAll.Location = new System.Drawing.Point(106, 494);
			this.waveformControlChAll.Name = "waveformControlChAll";
			this.tlpWaveforms.SetRowSpan(this.waveformControlChAll, 2);
			this.waveformControlChAll.Size = new System.Drawing.Size(722, 116);
			this.waveformControlChAll.TabIndex = 14;
			// 
			// waveformControlCh4
			// 
			this.waveformControlCh4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.waveformControlCh4.BackColor = System.Drawing.Color.Black;
			this.waveformControlCh4.ForeColor = System.Drawing.Color.White;
			this.waveformControlCh4.Location = new System.Drawing.Point(106, 372);
			this.waveformControlCh4.Name = "waveformControlCh4";
			this.tlpWaveforms.SetRowSpan(this.waveformControlCh4, 2);
			this.waveformControlCh4.Size = new System.Drawing.Size(722, 116);
			this.waveformControlCh4.TabIndex = 13;
			// 
			// waveformControlCh1
			// 
			this.waveformControlCh1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.waveformControlCh1.BackColor = System.Drawing.Color.Black;
			this.waveformControlCh1.ForeColor = System.Drawing.Color.Tomato;
			this.waveformControlCh1.Location = new System.Drawing.Point(106, 6);
			this.waveformControlCh1.Name = "waveformControlCh1";
			this.tlpWaveforms.SetRowSpan(this.waveformControlCh1, 2);
			this.waveformControlCh1.Size = new System.Drawing.Size(722, 116);
			this.waveformControlCh1.TabIndex = 8;
			// 
			// waveformControlCh2
			// 
			this.waveformControlCh2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.waveformControlCh2.BackColor = System.Drawing.Color.Black;
			this.waveformControlCh2.ForeColor = System.Drawing.Color.Gold;
			this.waveformControlCh2.Location = new System.Drawing.Point(106, 128);
			this.waveformControlCh2.Name = "waveformControlCh2";
			this.tlpWaveforms.SetRowSpan(this.waveformControlCh2, 2);
			this.waveformControlCh2.Size = new System.Drawing.Size(722, 116);
			this.waveformControlCh2.TabIndex = 9;
			// 
			// waveformControlCh3
			// 
			this.waveformControlCh3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.waveformControlCh3.BackColor = System.Drawing.Color.Black;
			this.waveformControlCh3.ForeColor = System.Drawing.Color.Turquoise;
			this.waveformControlCh3.Location = new System.Drawing.Point(106, 250);
			this.waveformControlCh3.Name = "waveformControlCh3";
			this.tlpWaveforms.SetRowSpan(this.waveformControlCh3, 2);
			this.waveformControlCh3.Size = new System.Drawing.Size(722, 116);
			this.waveformControlCh3.TabIndex = 10;
			// 
			// SoundDebuggerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(834, 616);
			this.Controls.Add(this.tlpWaveforms);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SoundDebuggerForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Sound Debug";
			this.tlpWaveforms.ResumeLayout(false);
			this.tlpWaveforms.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.TableLayoutPanel tlpWaveforms;
		private Sound.WaveformControl waveformControlCh1;
		private System.Windows.Forms.Label lblChannel1;
		private System.Windows.Forms.Label lblChannel2;
		private Sound.WaveformControl waveformControlCh2;
		private Sound.WaveformControl waveformControlCh3;
		private Sound.WaveformControl waveformControlCh4;
		private System.Windows.Forms.Label lblChannel3;
		private System.Windows.Forms.Label lblChannel4;
		private Sound.WaveformControl waveformControlChAll;
		private System.Windows.Forms.Label lblChannelAll;
		private System.Windows.Forms.Label lblChannel4Muted;
		private System.Windows.Forms.Label lblChannel3Muted;
		private System.Windows.Forms.Label lblChannel2Muted;
		private System.Windows.Forms.Label lblChannel1Muted;
	}
}