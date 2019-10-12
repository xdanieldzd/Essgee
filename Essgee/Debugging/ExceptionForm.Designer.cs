namespace Essgee.Debugging
{
	partial class ExceptionForm
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
			this.btnOkay = new System.Windows.Forms.Button();
			this.lblText = new System.Windows.Forms.Label();
			this.pbIcon = new System.Windows.Forms.PictureBox();
			this.tlpControls = new System.Windows.Forms.TableLayoutPanel();
			this.btnDetails = new System.Windows.Forms.Button();
			this.txtDetails = new System.Windows.Forms.TextBox();
			this.pnlContainer = new System.Windows.Forms.Panel();
			((System.ComponentModel.ISupportInitialize)(this.pbIcon)).BeginInit();
			this.tlpControls.SuspendLayout();
			this.pnlContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOkay
			// 
			this.btnOkay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnOkay.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOkay.Location = new System.Drawing.Point(357, 25);
			this.btnOkay.Name = "btnOkay";
			this.btnOkay.Size = new System.Drawing.Size(100, 23);
			this.btnOkay.TabIndex = 1;
			this.btnOkay.Text = "&OK";
			this.btnOkay.UseVisualStyleBackColor = true;
			// 
			// lblText
			// 
			this.lblText.AutoSize = true;
			this.tlpControls.SetColumnSpan(this.lblText, 3);
			this.lblText.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lblText.Location = new System.Drawing.Point(25, 0);
			this.lblText.Margin = new System.Windows.Forms.Padding(3, 0, 3, 9);
			this.lblText.Name = "lblText";
			this.lblText.Size = new System.Drawing.Size(432, 13);
			this.lblText.TabIndex = 0;
			this.lblText.Text = "---";
			// 
			// pbIcon
			// 
			this.pbIcon.Location = new System.Drawing.Point(3, 3);
			this.pbIcon.Name = "pbIcon";
			this.pbIcon.Size = new System.Drawing.Size(16, 16);
			this.pbIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pbIcon.TabIndex = 2;
			this.pbIcon.TabStop = false;
			// 
			// tlpControls
			// 
			this.tlpControls.AutoSize = true;
			this.tlpControls.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tlpControls.ColumnCount = 4;
			this.tlpControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tlpControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpControls.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpControls.Controls.Add(this.lblText, 1, 0);
			this.tlpControls.Controls.Add(this.pbIcon, 0, 0);
			this.tlpControls.Controls.Add(this.btnOkay, 3, 1);
			this.tlpControls.Controls.Add(this.btnDetails, 2, 1);
			this.tlpControls.Controls.Add(this.txtDetails, 0, 2);
			this.tlpControls.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tlpControls.Location = new System.Drawing.Point(0, 0);
			this.tlpControls.Name = "tlpControls";
			this.tlpControls.RowCount = 3;
			this.tlpControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpControls.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpControls.Size = new System.Drawing.Size(460, 257);
			this.tlpControls.TabIndex = 0;
			// 
			// btnDetails
			// 
			this.btnDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDetails.Location = new System.Drawing.Point(251, 25);
			this.btnDetails.Name = "btnDetails";
			this.btnDetails.Size = new System.Drawing.Size(100, 23);
			this.btnDetails.TabIndex = 2;
			this.btnDetails.Text = "---";
			this.btnDetails.UseVisualStyleBackColor = true;
			// 
			// txtDetails
			// 
			this.txtDetails.BackColor = System.Drawing.SystemColors.Window;
			this.tlpControls.SetColumnSpan(this.txtDetails, 4);
			this.txtDetails.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtDetails.Location = new System.Drawing.Point(3, 54);
			this.txtDetails.Multiline = true;
			this.txtDetails.Name = "txtDetails";
			this.txtDetails.ReadOnly = true;
			this.txtDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtDetails.Size = new System.Drawing.Size(454, 200);
			this.txtDetails.TabIndex = 3;
			// 
			// pnlContainer
			// 
			this.pnlContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.pnlContainer.AutoSize = true;
			this.pnlContainer.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.pnlContainer.Controls.Add(this.tlpControls);
			this.pnlContainer.Location = new System.Drawing.Point(12, 12);
			this.pnlContainer.Name = "pnlContainer";
			this.pnlContainer.Size = new System.Drawing.Size(460, 257);
			this.pnlContainer.TabIndex = 4;
			// 
			// ExceptionForm
			// 
			this.AcceptButton = this.btnOkay;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(484, 282);
			this.Controls.Add(this.pnlContainer);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExceptionForm";
			this.Text = "---";
			((System.ComponentModel.ISupportInitialize)(this.pbIcon)).EndInit();
			this.tlpControls.ResumeLayout(false);
			this.tlpControls.PerformLayout();
			this.pnlContainer.ResumeLayout(false);
			this.pnlContainer.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOkay;
		private System.Windows.Forms.Label lblText;
		private System.Windows.Forms.PictureBox pbIcon;
		private System.Windows.Forms.TableLayoutPanel tlpControls;
		private System.Windows.Forms.Panel pnlContainer;
		private System.Windows.Forms.Button btnDetails;
		private System.Windows.Forms.TextBox txtDetails;
	}
}