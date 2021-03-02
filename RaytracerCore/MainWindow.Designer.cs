
namespace RaytracerCore
{
	partial class MainWindow
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.openSceneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveOutputToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.labelStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.labelSample = new System.Windows.Forms.ToolStripStatusLabel();
			this.labelProgress = new System.Windows.Forms.ToolStripStatusLabel();
			this.barProgress = new System.Windows.Forms.ToolStripProgressBar();
			this.mainPanel = new System.Windows.Forms.TableLayoutPanel();
			this.sceneStatusPanel = new System.Windows.Forms.TableLayoutPanel();
			this.labelSceneTitle = new System.Windows.Forms.Label();
			this.labelCameraTitle = new System.Windows.Forms.Label();
			this.labelScene = new System.Windows.Forms.Label();
			this.comboCamera = new System.Windows.Forms.ComboBox();
			this.sliderExposure = new System.Windows.Forms.TrackBar();
			this.labelExposureTitle = new System.Windows.Forms.Label();
			this.numericExposure = new System.Windows.Forms.NumericUpDown();
			this.textBackground = new System.Windows.Forms.TextBox();
			this.labelBackgroundTitle = new System.Windows.Forms.Label();
			this.buttonReload = new System.Windows.Forms.Button();
			this.checkDebug = new System.Windows.Forms.CheckBox();
			this.buttonPause = new System.Windows.Forms.Button();
			this.panelPreview = new System.Windows.Forms.Panel();
			this.renderedImageBox = new System.Windows.Forms.PictureBox();
			this.menuStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.mainPanel.SuspendLayout();
			this.sceneStatusPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.sliderExposure)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericExposure)).BeginInit();
			this.panelPreview.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.renderedImageBox)).BeginInit();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileMenu});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			this.menuStrip.Size = new System.Drawing.Size(908, 24);
			this.menuStrip.TabIndex = 1;
			this.menuStrip.Text = "menuStrip1";
			// 
			// fileMenu
			// 
			this.fileMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openSceneMenuItem,
            this.saveOutputToolStripMenuItem});
			this.fileMenu.Name = "fileMenu";
			this.fileMenu.Size = new System.Drawing.Size(37, 20);
			this.fileMenu.Text = "File";
			// 
			// openSceneMenuItem
			// 
			this.openSceneMenuItem.Name = "openSceneMenuItem";
			this.openSceneMenuItem.Size = new System.Drawing.Size(146, 22);
			this.openSceneMenuItem.Text = "Open scene...";
			this.openSceneMenuItem.Click += new System.EventHandler(this.openSceneMenuItem_Click);
			// 
			// saveOutputToolStripMenuItem
			// 
			this.saveOutputToolStripMenuItem.Name = "saveOutputToolStripMenuItem";
			this.saveOutputToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
			this.saveOutputToolStripMenuItem.Text = "Save output...";
			this.saveOutputToolStripMenuItem.Click += new System.EventHandler(this.saveOutputToolStripMenuItem_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelStatus,
            this.labelSample,
            this.labelProgress,
            this.barProgress});
			this.statusStrip.Location = new System.Drawing.Point(0, 583);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
			this.statusStrip.Size = new System.Drawing.Size(908, 24);
			this.statusStrip.TabIndex = 2;
			this.statusStrip.Text = "statusStrip1";
			// 
			// labelStatus
			// 
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(689, 19);
			this.labelStatus.Spring = true;
			this.labelStatus.Text = "renderStatus";
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelSample
			// 
			this.labelSample.Name = "labelSample";
			this.labelSample.Size = new System.Drawing.Size(0, 19);
			// 
			// labelProgress
			// 
			this.labelProgress.Name = "labelProgress";
			this.labelProgress.Size = new System.Drawing.Size(23, 19);
			this.labelProgress.Text = "0%";
			// 
			// barProgress
			// 
			this.barProgress.Margin = new System.Windows.Forms.Padding(1, 3, 3, 3);
			this.barProgress.Name = "barProgress";
			this.barProgress.Size = new System.Drawing.Size(175, 18);
			// 
			// mainPanel
			// 
			this.mainPanel.ColumnCount = 1;
			this.mainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainPanel.Controls.Add(this.sceneStatusPanel, 0, 0);
			this.mainPanel.Controls.Add(this.panelPreview, 0, 1);
			this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainPanel.Location = new System.Drawing.Point(0, 24);
			this.mainPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.mainPanel.Name = "mainPanel";
			this.mainPanel.RowCount = 2;
			this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this.mainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainPanel.Size = new System.Drawing.Size(908, 559);
			this.mainPanel.TabIndex = 3;
			// 
			// sceneStatusPanel
			// 
			this.sceneStatusPanel.ColumnCount = 12;
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 153F));
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 88F));
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.sceneStatusPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.sceneStatusPanel.Controls.Add(this.labelSceneTitle, 0, 0);
			this.sceneStatusPanel.Controls.Add(this.labelCameraTitle, 4, 0);
			this.sceneStatusPanel.Controls.Add(this.labelScene, 1, 0);
			this.sceneStatusPanel.Controls.Add(this.comboCamera, 5, 0);
			this.sceneStatusPanel.Controls.Add(this.sliderExposure, 7, 0);
			this.sceneStatusPanel.Controls.Add(this.labelExposureTitle, 6, 0);
			this.sceneStatusPanel.Controls.Add(this.numericExposure, 8, 0);
			this.sceneStatusPanel.Controls.Add(this.textBackground, 10, 0);
			this.sceneStatusPanel.Controls.Add(this.labelBackgroundTitle, 9, 0);
			this.sceneStatusPanel.Controls.Add(this.buttonReload, 2, 0);
			this.sceneStatusPanel.Controls.Add(this.checkDebug, 11, 0);
			this.sceneStatusPanel.Controls.Add(this.buttonPause, 3, 0);
			this.sceneStatusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sceneStatusPanel.Location = new System.Drawing.Point(0, 0);
			this.sceneStatusPanel.Margin = new System.Windows.Forms.Padding(0);
			this.sceneStatusPanel.Name = "sceneStatusPanel";
			this.sceneStatusPanel.RowCount = 1;
			this.sceneStatusPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.sceneStatusPanel.Size = new System.Drawing.Size(908, 28);
			this.sceneStatusPanel.TabIndex = 0;
			// 
			// labelSceneTitle
			// 
			this.labelSceneTitle.AutoSize = true;
			this.labelSceneTitle.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelSceneTitle.Location = new System.Drawing.Point(4, 0);
			this.labelSceneTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelSceneTitle.Name = "labelSceneTitle";
			this.labelSceneTitle.Padding = new System.Windows.Forms.Padding(0, 3, 4, 3);
			this.labelSceneTitle.Size = new System.Drawing.Size(45, 28);
			this.labelSceneTitle.TabIndex = 1;
			this.labelSceneTitle.Text = "Scene:";
			this.labelSceneTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelCameraTitle
			// 
			this.labelCameraTitle.AutoSize = true;
			this.labelCameraTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelCameraTitle.Location = new System.Drawing.Point(137, 0);
			this.labelCameraTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelCameraTitle.Name = "labelCameraTitle";
			this.labelCameraTitle.Padding = new System.Windows.Forms.Padding(0, 3, 4, 3);
			this.labelCameraTitle.Size = new System.Drawing.Size(55, 28);
			this.labelCameraTitle.TabIndex = 0;
			this.labelCameraTitle.Text = "Camera:";
			this.labelCameraTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelScene
			// 
			this.labelScene.AutoSize = true;
			this.labelScene.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelScene.Location = new System.Drawing.Point(57, 0);
			this.labelScene.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelScene.Name = "labelScene";
			this.labelScene.Size = new System.Drawing.Size(1, 28);
			this.labelScene.TabIndex = 2;
			this.labelScene.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// comboCamera
			// 
			this.comboCamera.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboCamera.Items.AddRange(new object[] {
            "Camera 0"});
			this.comboCamera.Location = new System.Drawing.Point(200, 3);
			this.comboCamera.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.comboCamera.Name = "comboCamera";
			this.comboCamera.Size = new System.Drawing.Size(145, 23);
			this.comboCamera.TabIndex = 3;
			this.comboCamera.Text = "Camera 0";
			this.comboCamera.SelectedIndexChanged += new System.EventHandler(this.comboCamera_SelectedIndexChanged);
			// 
			// sliderExposure
			// 
			this.sliderExposure.Dock = System.Windows.Forms.DockStyle.Fill;
			this.sliderExposure.Location = new System.Drawing.Point(419, 3);
			this.sliderExposure.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.sliderExposure.Maximum = 200;
			this.sliderExposure.Name = "sliderExposure";
			this.sliderExposure.Size = new System.Drawing.Size(121, 22);
			this.sliderExposure.TabIndex = 4;
			this.sliderExposure.TickFrequency = 50;
			this.sliderExposure.Scroll += new System.EventHandler(this.sliderExposure_Scroll);
			// 
			// labelExposureTitle
			// 
			this.labelExposureTitle.AutoSize = true;
			this.labelExposureTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelExposureTitle.Location = new System.Drawing.Point(353, 0);
			this.labelExposureTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelExposureTitle.Name = "labelExposureTitle";
			this.labelExposureTitle.Size = new System.Drawing.Size(58, 28);
			this.labelExposureTitle.TabIndex = 5;
			this.labelExposureTitle.Text = "Exposure:";
			this.labelExposureTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// numericExposure
			// 
			this.numericExposure.DecimalPlaces = 3;
			this.numericExposure.Dock = System.Windows.Forms.DockStyle.Fill;
			this.numericExposure.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.numericExposure.Location = new System.Drawing.Point(548, 3);
			this.numericExposure.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.numericExposure.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
			this.numericExposure.Name = "numericExposure";
			this.numericExposure.Size = new System.Drawing.Size(80, 23);
			this.numericExposure.TabIndex = 6;
			this.numericExposure.ValueChanged += new System.EventHandler(this.numericExposure_ValueChanged);
			// 
			// textBackground
			// 
			this.textBackground.Location = new System.Drawing.Point(718, 3);
			this.textBackground.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.textBackground.Name = "textBackground";
			this.textBackground.Size = new System.Drawing.Size(69, 23);
			this.textBackground.TabIndex = 7;
			this.textBackground.Text = "00000000";
			this.textBackground.TextChanged += new System.EventHandler(this.textBackground_TextChanged);
			// 
			// labelBackgroundTitle
			// 
			this.labelBackgroundTitle.AutoSize = true;
			this.labelBackgroundTitle.Dock = System.Windows.Forms.DockStyle.Fill;
			this.labelBackgroundTitle.Location = new System.Drawing.Point(636, 0);
			this.labelBackgroundTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelBackgroundTitle.Name = "labelBackgroundTitle";
			this.labelBackgroundTitle.Size = new System.Drawing.Size(74, 28);
			this.labelBackgroundTitle.TabIndex = 8;
			this.labelBackgroundTitle.Text = "Background:";
			this.labelBackgroundTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// buttonReload
			// 
			this.buttonReload.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonReload.Location = new System.Drawing.Point(65, 0);
			this.buttonReload.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.buttonReload.Name = "buttonReload";
			this.buttonReload.Size = new System.Drawing.Size(28, 28);
			this.buttonReload.TabIndex = 9;
			this.buttonReload.Text = "⟳";
			this.buttonReload.UseVisualStyleBackColor = true;
			this.buttonReload.Click += new System.EventHandler(this.buttonReload_Click);
			// 
			// checkDebug
			// 
			this.checkDebug.AutoSize = true;
			this.checkDebug.Dock = System.Windows.Forms.DockStyle.Left;
			this.checkDebug.Location = new System.Drawing.Point(795, 3);
			this.checkDebug.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.checkDebug.Name = "checkDebug";
			this.checkDebug.Size = new System.Drawing.Size(61, 22);
			this.checkDebug.TabIndex = 10;
			this.checkDebug.Text = "Debug";
			this.checkDebug.UseVisualStyleBackColor = true;
			this.checkDebug.CheckedChanged += new System.EventHandler(this.checkDebug_CheckedChanged);
			// 
			// buttonPause
			// 
			this.buttonPause.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonPause.Location = new System.Drawing.Point(101, 0);
			this.buttonPause.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.buttonPause.Name = "buttonPause";
			this.buttonPause.Size = new System.Drawing.Size(28, 28);
			this.buttonPause.TabIndex = 11;
			this.buttonPause.Text = "P";
			this.buttonPause.UseVisualStyleBackColor = true;
			this.buttonPause.Click += new System.EventHandler(this.buttonPause_Click);
			// 
			// panelPreview
			// 
			this.panelPreview.AutoScroll = true;
			this.panelPreview.BackColor = System.Drawing.Color.Green;
			this.panelPreview.Controls.Add(this.renderedImageBox);
			this.panelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panelPreview.Location = new System.Drawing.Point(4, 31);
			this.panelPreview.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.panelPreview.Name = "panelPreview";
			this.panelPreview.Size = new System.Drawing.Size(900, 525);
			this.panelPreview.TabIndex = 2;
			// 
			// renderedImageBox
			// 
			this.renderedImageBox.Location = new System.Drawing.Point(0, 0);
			this.renderedImageBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.renderedImageBox.Name = "renderedImageBox";
			this.renderedImageBox.Size = new System.Drawing.Size(350, 346);
			this.renderedImageBox.TabIndex = 1;
			this.renderedImageBox.TabStop = false;
			this.renderedImageBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.renderedImageBox_Click);
			this.renderedImageBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.renderedImageBox_MouseMove);
			// 
			// MainWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(908, 607);
			this.Controls.Add(this.mainPanel);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.menuStrip);
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "MainWindow";
			this.Text = "Raytracer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.mainPanel.ResumeLayout(false);
			this.sceneStatusPanel.ResumeLayout(false);
			this.sceneStatusPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.sliderExposure)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericExposure)).EndInit();
			this.panelPreview.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.renderedImageBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.MenuStrip menuStrip;
		public System.Windows.Forms.ToolStripMenuItem fileMenu;
		public System.Windows.Forms.ToolStripMenuItem openSceneMenuItem;
		public System.Windows.Forms.ToolStripMenuItem saveOutputToolStripMenuItem;
		public System.Windows.Forms.ToolStripStatusLabel labelStatus;
		public System.Windows.Forms.TableLayoutPanel mainPanel;
		public System.Windows.Forms.TableLayoutPanel sceneStatusPanel;
		public System.Windows.Forms.Label labelSceneTitle;
		public System.Windows.Forms.Label labelScene;
		public System.Windows.Forms.Button buttonReload;
		public System.Windows.Forms.Label labelCameraTitle;
		public System.Windows.Forms.ComboBox comboCamera;
		public System.Windows.Forms.Label labelExposureTitle;
		public System.Windows.Forms.TrackBar sliderExposure;
		public System.Windows.Forms.NumericUpDown numericExposure;
		public System.Windows.Forms.Label labelBackgroundTitle;
		public System.Windows.Forms.TextBox textBackground;
		public System.Windows.Forms.CheckBox checkDebug;
		public System.Windows.Forms.Panel panelPreview;
		public System.Windows.Forms.PictureBox renderedImageBox;
		public System.Windows.Forms.StatusStrip statusStrip;
		public System.Windows.Forms.ToolStripProgressBar barProgress;
		public System.Windows.Forms.ToolStripStatusLabel labelProgress;
		public System.Windows.Forms.ToolStripStatusLabel labelSample;
		private System.Windows.Forms.Button buttonPause;
	}
}

