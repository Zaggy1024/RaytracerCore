
namespace RaytracerCore.Inspector
{
	partial class RayInspector
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
			this.layoutRows = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.comboTraces = new System.Windows.Forms.ComboBox();
			this.inputY = new System.Windows.Forms.NumericUpDown();
			this.inputX = new System.Windows.Forms.NumericUpDown();
			this.buttonRun = new System.Windows.Forms.Button();
			this.labelX = new System.Windows.Forms.Label();
			this.labelY = new System.Windows.Forms.Label();
			this.labelSamples = new System.Windows.Forms.Label();
			this.inputSamples = new System.Windows.Forms.NumericUpDown();
			this.splitTables = new System.Windows.Forms.SplitContainer();
			this.listRays = new System.Windows.Forms.ListView();
			this.columnNumber = new System.Windows.Forms.ColumnHeader();
			this.columnPrimitive = new System.Windows.Forms.ColumnHeader();
			this.columnParent = new System.Windows.Forms.ColumnHeader();
			this.columnResult = new System.Windows.Forms.ColumnHeader();
			this.treeRayProperties = new System.Windows.Forms.TreeView();
			this.layoutRows.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.inputY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.inputX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.inputSamples)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.splitTables)).BeginInit();
			this.splitTables.Panel1.SuspendLayout();
			this.splitTables.Panel2.SuspendLayout();
			this.splitTables.SuspendLayout();
			this.SuspendLayout();
			// 
			// layoutRows
			// 
			this.layoutRows.ColumnCount = 1;
			this.layoutRows.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutRows.Controls.Add(this.tableLayoutPanel1, 0, 0);
			this.layoutRows.Controls.Add(this.splitTables, 0, 1);
			this.layoutRows.Dock = System.Windows.Forms.DockStyle.Fill;
			this.layoutRows.Location = new System.Drawing.Point(0, 0);
			this.layoutRows.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.layoutRows.Name = "layoutRows";
			this.layoutRows.RowCount = 2;
			this.layoutRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
			this.layoutRows.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.layoutRows.Size = new System.Drawing.Size(786, 440);
			this.layoutRows.TabIndex = 0;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 8;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.comboTraces, 7, 0);
			this.tableLayoutPanel1.Controls.Add(this.inputY, 3, 0);
			this.tableLayoutPanel1.Controls.Add(this.inputX, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.buttonRun, 6, 0);
			this.tableLayoutPanel1.Controls.Add(this.labelX, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.labelY, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.labelSamples, 4, 0);
			this.tableLayoutPanel1.Controls.Add(this.inputSamples, 5, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 1;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(786, 32);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// comboTraces
			// 
			this.comboTraces.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboTraces.FormattingEnabled = true;
			this.comboTraces.Location = new System.Drawing.Point(418, 3);
			this.comboTraces.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.comboTraces.Name = "comboTraces";
			this.comboTraces.Size = new System.Drawing.Size(364, 23);
			this.comboTraces.TabIndex = 0;
			this.comboTraces.SelectedIndexChanged += new System.EventHandler(this.comboTraces_SelectedIndexChanged);
			// 
			// inputY
			// 
			this.inputY.Location = new System.Drawing.Point(155, 3);
			this.inputY.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.inputY.Name = "inputY";
			this.inputY.Size = new System.Drawing.Size(93, 23);
			this.inputY.TabIndex = 1;
			// 
			// inputX
			// 
			this.inputX.Location = new System.Drawing.Point(29, 3);
			this.inputX.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.inputX.Name = "inputX";
			this.inputX.Size = new System.Drawing.Size(93, 23);
			this.inputX.TabIndex = 2;
			// 
			// buttonRun
			// 
			this.buttonRun.Location = new System.Drawing.Point(384, 3);
			this.buttonRun.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.buttonRun.Name = "buttonRun";
			this.buttonRun.Size = new System.Drawing.Size(26, 25);
			this.buttonRun.TabIndex = 3;
			this.buttonRun.Text = "➔";
			this.buttonRun.UseVisualStyleBackColor = true;
			this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
			// 
			// labelX
			// 
			this.labelX.AutoSize = true;
			this.labelX.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelX.Location = new System.Drawing.Point(4, 0);
			this.labelX.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelX.Name = "labelX";
			this.labelX.Size = new System.Drawing.Size(17, 32);
			this.labelX.TabIndex = 4;
			this.labelX.Text = "X:";
			this.labelX.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelY
			// 
			this.labelY.AutoSize = true;
			this.labelY.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelY.Location = new System.Drawing.Point(130, 0);
			this.labelY.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelY.Name = "labelY";
			this.labelY.Size = new System.Drawing.Size(17, 32);
			this.labelY.TabIndex = 5;
			this.labelY.Text = "Y:";
			this.labelY.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// labelSamples
			// 
			this.labelSamples.AutoSize = true;
			this.labelSamples.Dock = System.Windows.Forms.DockStyle.Left;
			this.labelSamples.Location = new System.Drawing.Point(256, 0);
			this.labelSamples.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.labelSamples.Name = "labelSamples";
			this.labelSamples.Size = new System.Drawing.Size(54, 32);
			this.labelSamples.TabIndex = 6;
			this.labelSamples.Text = "Samples:";
			this.labelSamples.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// inputSamples
			// 
			this.inputSamples.Location = new System.Drawing.Point(318, 3);
			this.inputSamples.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.inputSamples.Name = "inputSamples";
			this.inputSamples.Size = new System.Drawing.Size(58, 23);
			this.inputSamples.TabIndex = 7;
			this.inputSamples.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
			// 
			// splitTables
			// 
			this.splitTables.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitTables.Location = new System.Drawing.Point(4, 35);
			this.splitTables.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.splitTables.Name = "splitTables";
			// 
			// splitTables.Panel1
			// 
			this.splitTables.Panel1.Controls.Add(this.listRays);
			// 
			// splitTables.Panel2
			// 
			this.splitTables.Panel2.Controls.Add(this.treeRayProperties);
			this.splitTables.Size = new System.Drawing.Size(778, 402);
			this.splitTables.SplitterDistance = 478;
			this.splitTables.SplitterWidth = 5;
			this.splitTables.TabIndex = 1;
			// 
			// listRays
			// 
			this.listRays.AllowColumnReorder = true;
			this.listRays.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnNumber,
            this.columnPrimitive,
            this.columnParent,
            this.columnResult});
			this.listRays.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listRays.FullRowSelect = true;
			this.listRays.HideSelection = false;
			this.listRays.Location = new System.Drawing.Point(0, 0);
			this.listRays.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.listRays.MultiSelect = false;
			this.listRays.Name = "listRays";
			this.listRays.Size = new System.Drawing.Size(478, 402);
			this.listRays.TabIndex = 0;
			this.listRays.UseCompatibleStateImageBehavior = false;
			this.listRays.View = System.Windows.Forms.View.Details;
			this.listRays.SelectedIndexChanged += new System.EventHandler(this.listRays_SelectedIndexChanged);
			// 
			// columnNumber
			// 
			this.columnNumber.Text = "#";
			this.columnNumber.Width = 30;
			// 
			// columnPrimitive
			// 
			this.columnPrimitive.Text = "Primitive";
			this.columnPrimitive.Width = 80;
			// 
			// columnParent
			// 
			this.columnParent.Text = "Parent";
			this.columnParent.Width = 80;
			// 
			// columnResult
			// 
			this.columnResult.Text = "Result";
			this.columnResult.Width = 120;
			// 
			// treeRayProperties
			// 
			this.treeRayProperties.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeRayProperties.Location = new System.Drawing.Point(0, 0);
			this.treeRayProperties.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.treeRayProperties.Name = "treeRayProperties";
			this.treeRayProperties.Size = new System.Drawing.Size(295, 402);
			this.treeRayProperties.TabIndex = 0;
			// 
			// RayInspector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(786, 440);
			this.Controls.Add(this.layoutRows);
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "RayInspector";
			this.Text = "Ray Inspector";
			this.layoutRows.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.inputY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.inputX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.inputSamples)).EndInit();
			this.splitTables.Panel1.ResumeLayout(false);
			this.splitTables.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitTables)).EndInit();
			this.splitTables.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel layoutRows;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ComboBox comboTraces;
		private System.Windows.Forms.SplitContainer splitTables;
		private System.Windows.Forms.NumericUpDown inputY;
		private System.Windows.Forms.NumericUpDown inputX;
		private System.Windows.Forms.Button buttonRun;
		private System.Windows.Forms.TreeView treeRayProperties;
		private System.Windows.Forms.Label labelX;
		private System.Windows.Forms.Label labelY;
		private System.Windows.Forms.Label labelSamples;
		private System.Windows.Forms.NumericUpDown inputSamples;
		private System.Windows.Forms.ListView listRays;
		private System.Windows.Forms.ColumnHeader columnNumber;
		private System.Windows.Forms.ColumnHeader columnPrimitive;
		private System.Windows.Forms.ColumnHeader columnResult;
		private System.Windows.Forms.ColumnHeader columnParent;
	}
}