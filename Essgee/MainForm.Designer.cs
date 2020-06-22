namespace Essgee
{
	partial class MainForm
	{
		/// <summary>
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openROMToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.takeScreenshotToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.emulationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pauseToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stopToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
			this.loadStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dummy1ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.dummy2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
			this.powerOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.limitFPSToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.showFPSToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripSeparator();
			this.automaticPauseToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
			this.graphicsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.screenSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.sizeModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.shadersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
			this.toggleLayersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.soundToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.muteToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.lowPassFilterToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
			this.startRecordingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.stopRecordingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripSeparator();
			this.sampleRateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripSeparator();
			this.enableChannelsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.inputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.useXInputControllerToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.enableXInputVibrationToolStripMenuItem = new Essgee.Utilities.BindableToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
			this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.soundDebuggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.tsslEmulationStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.ofdOpenROM = new System.Windows.Forms.OpenFileDialog();
			this.renderControl = new Essgee.Graphics.RenderControl();
			this.sfdSaveWavRecording = new System.Windows.Forms.SaveFileDialog();
			this.menuStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.emulationToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.debugToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(496, 24);
			this.menuStrip.TabIndex = 1;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openROMToolStripMenuItem,
            this.toolStripMenuItem1,
            this.recentFilesToolStripMenuItem,
            this.toolStripMenuItem2,
            this.takeScreenshotToolStripMenuItem,
            this.toolStripMenuItem6,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openROMToolStripMenuItem
			// 
			this.openROMToolStripMenuItem.Name = "openROMToolStripMenuItem";
			this.openROMToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.openROMToolStripMenuItem.Text = "&Open ROM...";
			this.openROMToolStripMenuItem.Click += new System.EventHandler(this.openROMToolStripMenuItem_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(157, 6);
			// 
			// recentFilesToolStripMenuItem
			// 
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.recentFilesToolStripMenuItem.Text = "&Recent Files...";
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(157, 6);
			// 
			// takeScreenshotToolStripMenuItem
			// 
			this.takeScreenshotToolStripMenuItem.Enabled = false;
			this.takeScreenshotToolStripMenuItem.Name = "takeScreenshotToolStripMenuItem";
			this.takeScreenshotToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.takeScreenshotToolStripMenuItem.Text = "Take &Screenshot";
			this.takeScreenshotToolStripMenuItem.Click += new System.EventHandler(this.takeScreenshotToolStripMenuItem_Click);
			// 
			// toolStripMenuItem6
			// 
			this.toolStripMenuItem6.Name = "toolStripMenuItem6";
			this.toolStripMenuItem6.Size = new System.Drawing.Size(157, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// emulationToolStripMenuItem
			// 
			this.emulationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pauseToolStripMenuItem,
            this.resetToolStripMenuItem,
            this.stopToolStripMenuItem,
            this.toolStripMenuItem4,
            this.loadStateToolStripMenuItem,
            this.saveStateToolStripMenuItem,
            this.toolStripMenuItem8,
            this.powerOnToolStripMenuItem});
			this.emulationToolStripMenuItem.Name = "emulationToolStripMenuItem";
			this.emulationToolStripMenuItem.Size = new System.Drawing.Size(73, 20);
			this.emulationToolStripMenuItem.Text = "&Emulation";
			// 
			// pauseToolStripMenuItem
			// 
			this.pauseToolStripMenuItem.CheckOnClick = true;
			this.pauseToolStripMenuItem.Enabled = false;
			this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
			this.pauseToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.pauseToolStripMenuItem.Text = "&Pause";
			// 
			// resetToolStripMenuItem
			// 
			this.resetToolStripMenuItem.Enabled = false;
			this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
			this.resetToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.resetToolStripMenuItem.Text = "&Reset";
			this.resetToolStripMenuItem.Click += new System.EventHandler(this.resetToolStripMenuItem_Click);
			// 
			// stopToolStripMenuItem
			// 
			this.stopToolStripMenuItem.Enabled = false;
			this.stopToolStripMenuItem.Name = "stopToolStripMenuItem";
			this.stopToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.stopToolStripMenuItem.Text = "&Stop";
			this.stopToolStripMenuItem.Click += new System.EventHandler(this.stopToolStripMenuItem_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(135, 6);
			// 
			// loadStateToolStripMenuItem
			// 
			this.loadStateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dummy1ToolStripMenuItem});
			this.loadStateToolStripMenuItem.Enabled = false;
			this.loadStateToolStripMenuItem.Name = "loadStateToolStripMenuItem";
			this.loadStateToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.loadStateToolStripMenuItem.Text = "&Load State...";
			// 
			// dummy1ToolStripMenuItem
			// 
			this.dummy1ToolStripMenuItem.Name = "dummy1ToolStripMenuItem";
			this.dummy1ToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.dummy1ToolStripMenuItem.Text = "Dummy1";
			// 
			// saveStateToolStripMenuItem
			// 
			this.saveStateToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dummy2ToolStripMenuItem});
			this.saveStateToolStripMenuItem.Enabled = false;
			this.saveStateToolStripMenuItem.Name = "saveStateToolStripMenuItem";
			this.saveStateToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.saveStateToolStripMenuItem.Text = "Sa&ve State...";
			// 
			// dummy2ToolStripMenuItem
			// 
			this.dummy2ToolStripMenuItem.Name = "dummy2ToolStripMenuItem";
			this.dummy2ToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
			this.dummy2ToolStripMenuItem.Text = "Dummy2";
			// 
			// toolStripMenuItem8
			// 
			this.toolStripMenuItem8.Name = "toolStripMenuItem8";
			this.toolStripMenuItem8.Size = new System.Drawing.Size(135, 6);
			// 
			// powerOnToolStripMenuItem
			// 
			this.powerOnToolStripMenuItem.Name = "powerOnToolStripMenuItem";
			this.powerOnToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
			this.powerOnToolStripMenuItem.Text = "Power &On...";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.CheckOnClick = true;
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.limitFPSToolStripMenuItem,
            this.showFPSToolStripMenuItem,
            this.toolStripMenuItem12,
            this.automaticPauseToolStripMenuItem,
            this.toolStripMenuItem7,
            this.graphicsToolStripMenuItem,
            this.soundToolStripMenuItem,
            this.inputToolStripMenuItem,
            this.toolStripMenuItem5,
            this.settingsToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// limitFPSToolStripMenuItem
			// 
			this.limitFPSToolStripMenuItem.CheckOnClick = true;
			this.limitFPSToolStripMenuItem.Name = "limitFPSToolStripMenuItem";
			this.limitFPSToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.limitFPSToolStripMenuItem.Text = "Limit &FPS";
			// 
			// showFPSToolStripMenuItem
			// 
			this.showFPSToolStripMenuItem.CheckOnClick = true;
			this.showFPSToolStripMenuItem.Name = "showFPSToolStripMenuItem";
			this.showFPSToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.showFPSToolStripMenuItem.Text = "Show F&PS";
			// 
			// toolStripMenuItem12
			// 
			this.toolStripMenuItem12.Name = "toolStripMenuItem12";
			this.toolStripMenuItem12.Size = new System.Drawing.Size(177, 6);
			// 
			// automaticPauseToolStripMenuItem
			// 
			this.automaticPauseToolStripMenuItem.CheckOnClick = true;
			this.automaticPauseToolStripMenuItem.Name = "automaticPauseToolStripMenuItem";
			this.automaticPauseToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.automaticPauseToolStripMenuItem.Text = "&Automatic Pause";
			// 
			// toolStripMenuItem7
			// 
			this.toolStripMenuItem7.Name = "toolStripMenuItem7";
			this.toolStripMenuItem7.Size = new System.Drawing.Size(177, 6);
			// 
			// graphicsToolStripMenuItem
			// 
			this.graphicsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.screenSizeToolStripMenuItem,
            this.sizeModeToolStripMenuItem,
            this.shadersToolStripMenuItem,
            this.toolStripMenuItem3,
            this.toggleLayersToolStripMenuItem});
			this.graphicsToolStripMenuItem.Name = "graphicsToolStripMenuItem";
			this.graphicsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.graphicsToolStripMenuItem.Text = "&Graphics...";
			// 
			// screenSizeToolStripMenuItem
			// 
			this.screenSizeToolStripMenuItem.Name = "screenSizeToolStripMenuItem";
			this.screenSizeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.screenSizeToolStripMenuItem.Text = "Screen &Size...";
			// 
			// sizeModeToolStripMenuItem
			// 
			this.sizeModeToolStripMenuItem.Name = "sizeModeToolStripMenuItem";
			this.sizeModeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.sizeModeToolStripMenuItem.Text = "Si&ze Mode...";
			// 
			// shadersToolStripMenuItem
			// 
			this.shadersToolStripMenuItem.Name = "shadersToolStripMenuItem";
			this.shadersToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.shadersToolStripMenuItem.Text = "S&haders...";
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(177, 6);
			// 
			// toggleLayersToolStripMenuItem
			// 
			this.toggleLayersToolStripMenuItem.Enabled = false;
			this.toggleLayersToolStripMenuItem.Name = "toggleLayersToolStripMenuItem";
			this.toggleLayersToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.toggleLayersToolStripMenuItem.Text = "Toggle &Layers...";
			// 
			// soundToolStripMenuItem
			// 
			this.soundToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.muteToolStripMenuItem,
            this.lowPassFilterToolStripMenuItem,
            this.toolStripMenuItem9,
            this.startRecordingToolStripMenuItem,
            this.stopRecordingToolStripMenuItem,
            this.toolStripMenuItem10,
            this.sampleRateToolStripMenuItem,
            this.toolStripMenuItem11,
            this.enableChannelsToolStripMenuItem});
			this.soundToolStripMenuItem.Name = "soundToolStripMenuItem";
			this.soundToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.soundToolStripMenuItem.Text = "S&ound...";
			// 
			// muteToolStripMenuItem
			// 
			this.muteToolStripMenuItem.CheckOnClick = true;
			this.muteToolStripMenuItem.Name = "muteToolStripMenuItem";
			this.muteToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.muteToolStripMenuItem.Text = "&Mute";
			// 
			// lowPassFilterToolStripMenuItem
			// 
			this.lowPassFilterToolStripMenuItem.CheckOnClick = true;
			this.lowPassFilterToolStripMenuItem.Name = "lowPassFilterToolStripMenuItem";
			this.lowPassFilterToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.lowPassFilterToolStripMenuItem.Text = "&Low-Pass Filter";
			// 
			// toolStripMenuItem9
			// 
			this.toolStripMenuItem9.Name = "toolStripMenuItem9";
			this.toolStripMenuItem9.Size = new System.Drawing.Size(177, 6);
			// 
			// startRecordingToolStripMenuItem
			// 
			this.startRecordingToolStripMenuItem.Enabled = false;
			this.startRecordingToolStripMenuItem.Name = "startRecordingToolStripMenuItem";
			this.startRecordingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.startRecordingToolStripMenuItem.Text = "Start &Recording";
			this.startRecordingToolStripMenuItem.Click += new System.EventHandler(this.startRecordingToolStripMenuItem_Click);
			// 
			// stopRecordingToolStripMenuItem
			// 
			this.stopRecordingToolStripMenuItem.Enabled = false;
			this.stopRecordingToolStripMenuItem.Name = "stopRecordingToolStripMenuItem";
			this.stopRecordingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.stopRecordingToolStripMenuItem.Text = "S&top Recording";
			this.stopRecordingToolStripMenuItem.Click += new System.EventHandler(this.stopRecordingToolStripMenuItem_Click);
			// 
			// toolStripMenuItem10
			// 
			this.toolStripMenuItem10.Name = "toolStripMenuItem10";
			this.toolStripMenuItem10.Size = new System.Drawing.Size(177, 6);
			// 
			// sampleRateToolStripMenuItem
			// 
			this.sampleRateToolStripMenuItem.Name = "sampleRateToolStripMenuItem";
			this.sampleRateToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.sampleRateToolStripMenuItem.Text = "&Sample Rate...";
			// 
			// toolStripMenuItem11
			// 
			this.toolStripMenuItem11.Name = "toolStripMenuItem11";
			this.toolStripMenuItem11.Size = new System.Drawing.Size(177, 6);
			// 
			// enableChannelsToolStripMenuItem
			// 
			this.enableChannelsToolStripMenuItem.Enabled = false;
			this.enableChannelsToolStripMenuItem.Name = "enableChannelsToolStripMenuItem";
			this.enableChannelsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.enableChannelsToolStripMenuItem.Text = "Toggle &Channels...";
			// 
			// inputToolStripMenuItem
			// 
			this.inputToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.useXInputControllerToolStripMenuItem,
            this.enableXInputVibrationToolStripMenuItem});
			this.inputToolStripMenuItem.Name = "inputToolStripMenuItem";
			this.inputToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.inputToolStripMenuItem.Text = "&Input...";
			// 
			// useXInputControllerToolStripMenuItem
			// 
			this.useXInputControllerToolStripMenuItem.CheckOnClick = true;
			this.useXInputControllerToolStripMenuItem.Name = "useXInputControllerToolStripMenuItem";
			this.useXInputControllerToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.useXInputControllerToolStripMenuItem.Text = "Use &XInput Controller";
			// 
			// enableXInputVibrationToolStripMenuItem
			// 
			this.enableXInputVibrationToolStripMenuItem.CheckOnClick = true;
			this.enableXInputVibrationToolStripMenuItem.Name = "enableXInputVibrationToolStripMenuItem";
			this.enableXInputVibrationToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
			this.enableXInputVibrationToolStripMenuItem.Text = "Enable XInput &Vibration";
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(177, 6);
			// 
			// settingsToolStripMenuItem
			// 
			this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
			this.settingsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.settingsToolStripMenuItem.Text = "&Settings...";
			this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
			// 
			// debugToolStripMenuItem
			// 
			this.debugToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.soundDebuggerToolStripMenuItem});
			this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
			this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
			this.debugToolStripMenuItem.Text = "&Debug";
			// 
			// soundDebuggerToolStripMenuItem
			// 
			this.soundDebuggerToolStripMenuItem.Name = "soundDebuggerToolStripMenuItem";
			this.soundDebuggerToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
			this.soundDebuggerToolStripMenuItem.Text = "&Sound Debugger";
			this.soundDebuggerToolStripMenuItem.Click += new System.EventHandler(this.soundDebuggerToolStripMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
			this.aboutToolStripMenuItem.Text = "&About...";
			this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus,
            this.tsslEmulationStatus});
			this.statusStrip.Location = new System.Drawing.Point(0, 420);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(496, 22);
			this.statusStrip.TabIndex = 3;
			this.statusStrip.Text = "statusStrip1";
			// 
			// tsslStatus
			// 
			this.tsslStatus.Name = "tsslStatus";
			this.tsslStatus.Size = new System.Drawing.Size(459, 17);
			this.tsslStatus.Spring = true;
			this.tsslStatus.Text = "---";
			this.tsslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// tsslEmulationStatus
			// 
			this.tsslEmulationStatus.Name = "tsslEmulationStatus";
			this.tsslEmulationStatus.Size = new System.Drawing.Size(22, 17);
			this.tsslEmulationStatus.Text = "---";
			this.tsslEmulationStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// renderControl
			// 
			this.renderControl.BackColor = System.Drawing.Color.Black;
			this.renderControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.renderControl.Location = new System.Drawing.Point(0, 24);
			this.renderControl.Name = "renderControl";
			this.renderControl.Size = new System.Drawing.Size(496, 396);
			this.renderControl.TabIndex = 2;
			this.renderControl.VSync = false;
			this.renderControl.Render += new System.EventHandler<System.EventArgs>(this.renderControl_Render);
			this.renderControl.KeyDown += new System.Windows.Forms.KeyEventHandler(this.renderControl_KeyDown);
			this.renderControl.KeyUp += new System.Windows.Forms.KeyEventHandler(this.renderControl_KeyUp);
			this.renderControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.renderControl_MouseDown);
			this.renderControl.MouseMove += new System.Windows.Forms.MouseEventHandler(this.renderControl_MouseMove);
			this.renderControl.MouseUp += new System.Windows.Forms.MouseEventHandler(this.renderControl_MouseUp);
			this.renderControl.Resize += new System.EventHandler(this.renderControl_Resize);
			// 
			// sfdSaveWavRecording
			// 
			this.sfdSaveWavRecording.Filter = "WAVE Files (*.wav)|*.wav|All Files (*.*)|*.*";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(496, 442);
			this.Controls.Add(this.renderControl);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.menuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Shown += new System.EventHandler(this.MainForm_Shown);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.MenuStrip menuStrip;
		private Essgee.Graphics.RenderControl renderControl;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openROMToolStripMenuItem;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
		private System.Windows.Forms.OpenFileDialog ofdOpenROM;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem emulationToolStripMenuItem;
		private Essgee.Utilities.BindableToolStripMenuItem pauseToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private Essgee.Utilities.BindableToolStripMenuItem limitFPSToolStripMenuItem;
		private Essgee.Utilities.BindableToolStripMenuItem muteToolStripMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel tsslEmulationStatus;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem screenSizeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem shadersToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem sizeModeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem powerOnToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem takeScreenshotToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
		private System.Windows.Forms.ToolStripMenuItem stopToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem7;
		private System.Windows.Forms.ToolStripMenuItem toggleLayersToolStripMenuItem;
		private Essgee.Utilities.BindableToolStripMenuItem showFPSToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadStateToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveStateToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem8;
		private System.Windows.Forms.ToolStripMenuItem dummy1ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem dummy2ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enableChannelsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem soundDebuggerToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem soundToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem graphicsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem9;
		private Essgee.Utilities.BindableToolStripMenuItem lowPassFilterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem10;
		private System.Windows.Forms.ToolStripMenuItem sampleRateToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem startRecordingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem stopRecordingToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem11;
		private System.Windows.Forms.SaveFileDialog sfdSaveWavRecording;
		private System.Windows.Forms.ToolStripMenuItem inputToolStripMenuItem;
		private Essgee.Utilities.BindableToolStripMenuItem useXInputControllerToolStripMenuItem;
		private Essgee.Utilities.BindableToolStripMenuItem enableXInputVibrationToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem12;
		private Essgee.Utilities.BindableToolStripMenuItem automaticPauseToolStripMenuItem;
	}
}
