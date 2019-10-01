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
			this.waveformControl = new Essgee.Sound.WaveformControl();
			this.SuspendLayout();
			// 
			// waveformControl
			// 
			this.waveformControl.BackColor = System.Drawing.Color.Black;
			this.waveformControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.waveformControl.ForeColor = System.Drawing.Color.Turquoise;
			this.waveformControl.Location = new System.Drawing.Point(0, 0);
			this.waveformControl.Name = "waveformControl";
			this.waveformControl.Size = new System.Drawing.Size(754, 166);
			this.waveformControl.TabIndex = 5;
			// 
			// SoundDebuggerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(754, 166);
			this.Controls.Add(this.waveformControl);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SoundDebuggerForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Sound Debug";
			this.ResumeLayout(false);

		}

		#endregion

		private Sound.WaveformControl waveformControl;
	}
}