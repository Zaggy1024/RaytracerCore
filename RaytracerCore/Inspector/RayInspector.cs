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

		private TreeNode Node(string text, string name)
		{
			return new TreeNode(text) { Name = name };
		}

		private TreeNode Node(string name)
		{
			return Node(name, name);
		}

		private TreeNode VectorNode(Vec4D vector, string name)
		{
			TreeNode node = Node($"{name} (Vector {vector}):", name);
			node.Nodes.Add($"x: {vector.X}");
			node.Nodes.Add($"y: {vector.Y}");
			node.Nodes.Add($"z: {vector.Z}");
			node.Nodes.Add($"w: {vector.W}");
			node.Nodes.Add($"Length: {vector.Length}");
			return node;
		}

		private TreeNode ColorNode(DoubleColor color, string name)
		{
			TreeNode node = Node($"{name} (Color {color}):", name);
			node.Nodes.Add($"r: {color.R}");
			node.Nodes.Add($"g: {color.G}");
			node.Nodes.Add($"b: {color.B}");
			node.Nodes.Add($"Luminance: {color.Luminance}");
			return node;
		}

		private TreeNode ValueNode(object value, string name)
		{
			return new TreeNode($"{name}: {value}");
		}

		private TreeNode PrimitiveNode(Primitive obj, string name)
		{
			if (obj == null)
				return Node($"{name}: None", name);

			TreeNode node = Node($"{name} ({obj.GetType().Name})", name);
			node.Nodes.Add(ValueNode(Raytracer.Scene.Primitives.IndexOf(obj), "Index"));

			node.Nodes.Add(ColorNode(obj.Diffuse, "Diffuse"));

			node.Nodes.Add(ColorNode(obj.Specular, "Specular"));
			node.Nodes.Add(ValueNode(obj.Shininess, "Shininess"));

			node.Nodes.Add(ColorNode(obj.Refraction, "Refraction"));
			node.Nodes.Add(ValueNode(obj.RefractiveIndex, "Refractive Index"));

			node.Nodes.Add(ColorNode(obj.Emission, "Emission"));

			node.Expand();
			return node;
		}

		private TreeNode HitNode(Hit hit, string name)
		{
			string text = $"{name}";
			string extra = "";

			if (name != "Hit")
				extra += "Hit";

			if (hit == default)
			{
				if (extra != "")
					extra = " ";

				extra += "Missed";
			}

			if (extra != "")
				text += $"({extra})";

			text += ":";
			TreeNode node = Node(text, name);

			if (hit != default)
			{
				node.Nodes.Add(VectorNode(hit.Position, "Position"));
				node.Nodes.Add(ValueNode(hit.Distance, "Distance"));
				node.Nodes.Add(PrimitiveNode(hit.Primitive, "Object"));
				node.Nodes.Add(VectorNode(hit.Normal, "Normal"));
				node.Nodes.Add(ValueNode(hit.Inside, "Inside"));

#if TRACE
				node.Nodes.Add(ValueNode(hit.DebugText, "Debug"));
#endif
			}

			node.Expand();
			return node;
		}

		private void UpdateCurrentRay()
		{
			if (listRays.SelectedIndices.Count > 0)
			{
				Raytracer.DebugRay ray = CurrentTrace[listRays.SelectedIndices[0]];

				TreeNode hitNode = HitNode(ray.Hit, "Hit");
				hitNode.Expand();

				treeRayProperties.Nodes.Clear();
				treeRayProperties.Nodes.Add(hitNode);
				treeRayProperties.Nodes.Add(ValueNode(ray.Type, "Result"));
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
					primitiveItem.Text = ray.Hit.Primitive.GetType().Name;

					if (ray.Hit.Primitive.Parent != null)
						parentItem.Text = ray.Hit.Primitive.Parent.GetType().Name;
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
			lock (Raytracer.DebugRaytracer)
			{
				Raytracer.DebugRay[][] traces = new Raytracer.DebugRay[Samples][];

				for (int i = 0; i < inputSamples.Value; i++)
				{
					traces[i] = Raytracer.DebugRaytracer.GetDebugTrace(X, Y);
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
