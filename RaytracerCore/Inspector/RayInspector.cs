using System;
using System.Collections.Generic;
using System.Windows.Forms;

using RaytracerCore.Raytracing;
using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Vectors;

namespace RaytracerCore.Inspector
{
	/// <summary>
	/// A window to display information about how a specific output pixel is being path traced.
	/// </summary>
	public partial class RayInspector : Form
	{
		FullRaytracer Raytracer;

		readonly List<Raytracer.DebugRay[]> Traces = new List<Raytracer.DebugRay[]>();

		public RayInspector(FullRaytracer raytracer)
		{
			InitializeComponent();

			SetRaytracer(raytracer);

			Shown += (o, e) => RunTraces();
		}

		public void SetRaytracer(FullRaytracer raytracer)
		{
			Raytracer = raytracer;
			inputX.Minimum = 0;
			inputY.Minimum = 0;
			inputX.Maximum = raytracer.Scene.Width;
			inputY.Maximum = raytracer.Scene.Height;
		}

		private int X { get => (int)inputX.Value; }
		private int Y { get => (int)inputY.Value; }
		private int Samples { get => (int)inputSamples.Value; }

		private Raytracer.DebugRay[] CurrentTrace { get => Traces[comboTraces.SelectedIndex]; }

		public void SetPosition(int x, int y)
		{
			inputX.Value = x;
			inputY.Value = y;
		}

		private void UpdateCurrentRay()
		{
			if (listRays.SelectedIndices.Count > 0)
			{
				Raytracer.DebugRay ray = CurrentTrace[listRays.SelectedIndices[0]];

				TreeNode hitNode = Nodifier.CreateHit(ray.Hit, "Hit", Raytracer.Scene);
				hitNode.Expand();

				if (hitNode.Nodes.ContainsKey("Primitive"))
					hitNode.Nodes["Primitive"].Expand();

				treeRayProperties.Nodes.Clear();
				treeRayProperties.Nodes.Add(hitNode);
				treeRayProperties.Nodes.Add(Nodifier.CreateText(ray.Type, "Result"));

				if (!double.IsNaN(ray.FresnelRatio))
					treeRayProperties.Nodes.Add(Nodifier.CreateText(ray.FresnelRatio, "Fresnel Ratio"));
			}
			else
			{
				treeRayProperties.Nodes.Clear();
			}
		}

		private void UpdateCurrentTrace()
		{
			Raytracer.DebugRay[] trace = CurrentTrace;
			ListViewItem[] items = new ListViewItem[trace.Length];

			for (int i = 0; i < trace.Length; i++)
			{
				Raytracer.DebugRay ray = trace[i];
				ListViewItem rayItem = new ListViewItem(i.ToString());

				ListViewItem.ListViewSubItem primitiveItem = new ListViewItem.ListViewSubItem(rayItem, "None");
				rayItem.SubItems.Insert(columnPrimitive.Index, primitiveItem);

				ListViewItem.ListViewSubItem parentItem = new ListViewItem.ListViewSubItem(rayItem, "None");
				rayItem.SubItems.Insert(columnParent.Index, parentItem);

				if (ray.Hit != default)
				{
					primitiveItem.Text = ray.Hit.Primitive.Name;

					if (ray.Hit.Primitive.Parent != null)
						parentItem.Text = ray.Hit.Primitive.Parent.Name;
				}

				ListViewItem.ListViewSubItem resultItem = new ListViewItem.ListViewSubItem(rayItem, ray.Type.ToString());
				rayItem.SubItems.Insert(columnResult.Index, resultItem);

				items[i] = rayItem;
			}

			listRays.Items.Clear();
			listRays.Items.AddRange(items);

			listRays.SelectedIndices.Clear();
			listRays.SelectedIndices.Add(0);

			//UpdateCurrentRay();
		}

		private void UpdateTraces()
		{
			comboTraces.Items.Clear();

			for (int i = 0; i < Traces.Count; i++)
			{
				Raytracer.DebugRay[] trace = Traces[i];
				Raytracer.DebugRay first = trace[0];
				Raytracer.DebugRay last = trace[^1];

				string text = $"{i + 1}: {first.Type} Bounced {trace.Length - 1}";

				if (last.Hit != default)
					text += $" (Hit Emitting {last.Hit.Primitive.Emission.Luminance:0.##})";
				else
					text += " (Miss)";

				comboTraces.Items.Add(text);
			}

			comboTraces.SelectedIndex = 0;

			//UpdateCurrentTrace();
		}

		public void RunTraces()
		{
			lock (Raytracer.DebugPathtracer)
			{
				Raytracer.DebugRay[][] traces = new Raytracer.DebugRay[Samples][];

				for (int i = 0; i < inputSamples.Value; i++)
				{
					traces[i] = Raytracer.DebugPathtracer.GetDebugTrace(X, Y);
				}

				Traces.Clear();
				Traces.AddRange(traces);

				UpdateTraces();
			}
		}

		private void comboTraces_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateCurrentTrace();
		}

		private void listRays_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listRays.SelectedIndices.Count > 0)
				UpdateCurrentRay();
		}

		private void buttonRun_Click(object sender, EventArgs e)
		{
			RunTraces();
		}
	}
}
