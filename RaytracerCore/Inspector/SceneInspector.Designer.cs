
namespace RaytracerCore.Inspector
{
	partial class SceneInspector
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
			this.tabsViews = new System.Windows.Forms.TabControl();
			this.tabScene = new System.Windows.Forms.TabPage();
			this.layoutScene = new System.Windows.Forms.TableLayoutPanel();
			this.layoutSceneButtons = new System.Windows.Forms.TableLayoutPanel();
			this.checkHierarchy = new System.Windows.Forms.CheckBox();
			this.splitScene = new System.Windows.Forms.SplitContainer();
			this.treeScene = new System.Windows.Forms.TreeView();
			this.treePrimitive = new System.Windows.Forms.TreeView();
			this.tabBVH = new System.Windows.Forms.TabPage();
			this.splitBVH = new System.Windows.Forms.SplitContainer();
			this.treeBVH = new System.Windows.Forms.TreeView();
			this.treeBVHNode = new System.Windows.Forms.TreeView();
			this.layoutMain = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.checkOverlay = new System.Windows.Forms.CheckBox();
			this.checkSelection = new System.Windows.Forms.CheckBox();
			this.tabsViews.SuspendLayout();
			this.tabScene.SuspendLayout();
			this.layoutScene.SuspendLayout();
			this.layoutSceneButtons.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitScene)).BeginInit();
			this.splitScene.Panel1.SuspendLayout();
			this.splitScene.Panel2.SuspendLayout();
			this.splitScene.SuspendLayout();
			this.tabBVH.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitBVH)).BeginInit();
			this.splitBVH.Panel1.SuspendLayout();
			this.splitBVH.Panel2.SuspendLayout();
			this.splitBVH.SuspendLayout();
			this.layoutMain.SuspendLayout();
			this.tableLayoutPanel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabsViews
			// 
			this.tabsViews.Controls.Add(this.tabScene);
			this.tabsViews.Controls.Add(this.tabBVH);
			this.tabsViews.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabsViews.Location = new System.Drawing.Point(3, 34);
			this.tabsViews.Name = "tabsViews";
			this.tabsViews.SelectedIndex = 0;
			this.tabsViews.Size = new System.Drawing.Size(901, 551);
			this.tabsViews.TabIndex = 0;
			this.tabsViews.SelectedIndexChanged += new System.EventHandler(this.tabsViews_SelectedIndexChanged);
			// 
			// tabScene
			// 
			this.tabScene.Controls.Add(this.layoutScene);
			this.tabScene.Location = new System.Drawing.Point(4, 24);
			this.tabScene.Name = "tabScene";
			this.tabScene.Size = new System.Drawing.Size(893, 523);
			this.tabScene.TabIndex = 0;
			this.tabScene.Text = "Scene";
			this.tabScene.UseVisualStyleBackColor = true;
			// 
			// layoutScene
			// 
			this.layoutScene.ColumnCount = 1;
			this.layoutScene.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutScene.Controls.Add(this.layoutSceneButtons, 0, 0);
			this.layoutScene.Controls.Add(this.splitScene, 0, 1);
			this.layoutScene.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layoutScene.Location = new System.Drawing.Point(0, 0);
			this.layoutScene.Name = "layoutScene";
			this.layoutScene.RowCount = 2;
			this.layoutScene.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.layoutScene.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutScene.Size = new System.Drawing.Size(893, 523);
			this.layoutScene.TabIndex = 0;
			// 
			// layoutSceneButtons
			// 
			this.layoutSceneButtons.AutoSize = true;
			this.layoutSceneButtons.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.layoutSceneButtons.ColumnCount = 2;
			this.layoutSceneButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.layoutSceneButtons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutSceneButtons.Controls.Add(this.checkHierarchy, 0, 0);
			this.layoutSceneButtons.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layoutSceneButtons.Location = new System.Drawing.Point(3, 3);
			this.layoutSceneButtons.Name = "layoutSceneButtons";
			this.layoutSceneButtons.RowCount = 1;
			this.layoutSceneButtons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutSceneButtons.Size = new System.Drawing.Size(887, 25);
			this.layoutSceneButtons.TabIndex = 1;
			// 
			// checkHierarchy
			// 
			this.checkHierarchy.AutoSize = true;
			this.checkHierarchy.Checked = true;
			this.checkHierarchy.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkHierarchy.Location = new System.Drawing.Point(3, 3);
			this.checkHierarchy.Name = "checkHierarchy";
			this.checkHierarchy.Size = new System.Drawing.Size(152, 19);
			this.checkHierarchy.TabIndex = 0;
			this.checkHierarchy.Text = "Display object hierarchy";
			this.checkHierarchy.UseVisualStyleBackColor = true;
			this.checkHierarchy.CheckedChanged += new System.EventHandler(this.checkHierarchy_CheckedChanged);
			// 
			// splitScene
			// 
			this.splitScene.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitScene.Location = new System.Drawing.Point(3, 34);
			this.splitScene.Name = "splitScene";
			// 
			// splitScene.Panel1
			// 
			this.splitScene.Panel1.Controls.Add(this.treeScene);
			// 
			// splitScene.Panel2
			// 
			this.splitScene.Panel2.Controls.Add(this.treePrimitive);
			this.splitScene.Size = new System.Drawing.Size(887, 486);
			this.splitScene.SplitterDistance = 400;
			this.splitScene.TabIndex = 2;
			// 
			// treeScene
			// 
			this.treeScene.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeScene.Location = new System.Drawing.Point(0, 0);
			this.treeScene.Name = "treeScene";
			this.treeScene.Size = new System.Drawing.Size(400, 486);
			this.treeScene.TabIndex = 0;
			this.treeScene.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeScene_AfterSelect);
			// 
			// treePrimitive
			// 
			this.treePrimitive.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treePrimitive.Location = new System.Drawing.Point(0, 0);
			this.treePrimitive.Name = "treePrimitive";
			this.treePrimitive.Size = new System.Drawing.Size(483, 486);
			this.treePrimitive.TabIndex = 0;
			// 
			// tabBVH
			// 
			this.tabBVH.Controls.Add(this.splitBVH);
			this.tabBVH.Location = new System.Drawing.Point(4, 24);
			this.tabBVH.Name = "tabBVH";
			this.tabBVH.Padding = new System.Windows.Forms.Padding(3);
			this.tabBVH.Size = new System.Drawing.Size(893, 523);
			this.tabBVH.TabIndex = 1;
			this.tabBVH.Text = "Bounding Volume Hierarchy";
			this.tabBVH.UseVisualStyleBackColor = true;
			// 
			// splitBVH
			// 
			this.splitBVH.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitBVH.Location = new System.Drawing.Point(3, 3);
			this.splitBVH.Name = "splitBVH";
			// 
			// splitBVH.Panel1
			// 
			this.splitBVH.Panel1.Controls.Add(this.treeBVH);
			// 
			// splitBVH.Panel2
			// 
			this.splitBVH.Panel2.Controls.Add(this.treeBVHNode);
			this.splitBVH.Size = new System.Drawing.Size(887, 517);
			this.splitBVH.SplitterDistance = 397;
			this.splitBVH.TabIndex = 1;
			// 
			// treeBVH
			// 
			this.treeBVH.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeBVH.Location = new System.Drawing.Point(0, 0);
			this.treeBVH.Name = "treeBVH";
			this.treeBVH.Size = new System.Drawing.Size(397, 517);
			this.treeBVH.TabIndex = 0;
			this.treeBVH.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeBVH_AfterSelect);
			// 
			// treeBVHNode
			// 
			this.treeBVHNode.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeBVHNode.Location = new System.Drawing.Point(0, 0);
			this.treeBVHNode.Name = "treeBVHNode";
			this.treeBVHNode.Size = new System.Drawing.Size(486, 517);
			this.treeBVHNode.TabIndex = 0;
			// 
			// layoutMain
			// 
			this.layoutMain.ColumnCount = 1;
			this.layoutMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutMain.Controls.Add(this.tabsViews, 0, 1);
			this.layoutMain.Controls.Add(this.tableLayoutPanel2, 0, 0);
			this.layoutMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layoutMain.Location = new System.Drawing.Point(0, 0);
			this.layoutMain.Name = "layoutMain";
			this.layoutMain.RowCount = 2;
			this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.layoutMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutMain.Size = new System.Drawing.Size(907, 588);
			this.layoutMain.TabIndex = 1;
			// 
			// tableLayoutPanel2
			// 
			this.tableLayoutPanel2.AutoSize = true;
			this.tableLayoutPanel2.ColumnCount = 3;
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel2.Controls.Add(this.checkOverlay, 0, 0);
			this.tableLayoutPanel2.Controls.Add(this.checkSelection, 1, 0);
			this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
			this.tableLayoutPanel2.Name = "tableLayoutPanel2";
			this.tableLayoutPanel2.RowCount = 1;
			this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel2.Size = new System.Drawing.Size(901, 25);
			this.tableLayoutPanel2.TabIndex = 1;
			// 
			// checkOverlay
			// 
			this.checkOverlay.AutoSize = true;
			this.checkOverlay.Location = new System.Drawing.Point(3, 3);
			this.checkOverlay.Name = "checkOverlay";
			this.checkOverlay.Size = new System.Drawing.Size(102, 19);
			this.checkOverlay.TabIndex = 0;
			this.checkOverlay.Text = "Enable overlay";
			this.checkOverlay.UseVisualStyleBackColor = true;
			this.checkOverlay.CheckedChanged += new System.EventHandler(this.checkOverlay_CheckedChanged);
			// 
			// checkSelection
			// 
			this.checkSelection.AutoSize = true;
			this.checkSelection.Location = new System.Drawing.Point(111, 3);
			this.checkSelection.Name = "checkSelection";
			this.checkSelection.Size = new System.Drawing.Size(140, 19);
			this.checkSelection.TabIndex = 1;
			this.checkSelection.Text = "Display only selection";
			this.checkSelection.UseVisualStyleBackColor = true;
			this.checkSelection.CheckedChanged += new System.EventHandler(this.checkSelection_CheckedChanged);
			// 
			// SceneInspector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(907, 588);
			this.Controls.Add(this.layoutMain);
			this.Name = "SceneInspector";
			this.Text = "Scene Inspector";
			this.tabsViews.ResumeLayout(false);
			this.tabScene.ResumeLayout(false);
			this.layoutScene.ResumeLayout(false);
			this.layoutScene.PerformLayout();
			this.layoutSceneButtons.ResumeLayout(false);
			this.layoutSceneButtons.PerformLayout();
			this.splitScene.Panel1.ResumeLayout(false);
			this.splitScene.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitScene)).EndInit();
			this.splitScene.ResumeLayout(false);
			this.tabBVH.ResumeLayout(false);
			this.splitBVH.Panel1.ResumeLayout(false);
			this.splitBVH.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitBVH)).EndInit();
			this.splitBVH.ResumeLayout(false);
			this.layoutMain.ResumeLayout(false);
			this.layoutMain.PerformLayout();
			this.tableLayoutPanel2.ResumeLayout(false);
			this.tableLayoutPanel2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabControl tabsViews;
		private System.Windows.Forms.TabPage tabScene;
		private System.Windows.Forms.TabPage tabBVH;
		private System.Windows.Forms.TableLayoutPanel layoutScene;
		private System.Windows.Forms.TreeView treeScene;
		private System.Windows.Forms.TableLayoutPanel layoutSceneButtons;
		private System.Windows.Forms.CheckBox checkHierarchy;
		private System.Windows.Forms.SplitContainer splitScene;
		private System.Windows.Forms.TreeView treePrimitive;
		private System.Windows.Forms.SplitContainer splitBVH;
		private System.Windows.Forms.TreeView treeBVH;
		private System.Windows.Forms.TreeView treeBVHNode;
		private System.Windows.Forms.TableLayoutPanel layoutMain;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private System.Windows.Forms.CheckBox checkOverlay;
		private System.Windows.Forms.CheckBox checkSelection;
	}
}