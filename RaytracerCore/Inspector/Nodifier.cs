using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Reflection;

using RaytracerCore.Raytracing;
using RaytracerCore.Raytracing.Acceleration;
using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Vectors;

namespace RaytracerCore.Inspector
{
	public static class Nodifier
	{
		public static TreeNode Create(string text, string name)
		{
			return new TreeNode(text) { Name = name };
		}

		public static TreeNode Create(string name)
		{
			return Create(name, name);
		}

		public static TreeNode CreateVector(Vec4D vector, string name)
		{
			TreeNode node = Create($"{name} (Vector {vector}):", name);
			node.Tag = vector;

			node.Nodes.Add($"x: {vector.X}");
			node.Nodes.Add($"y: {vector.Y}");
			node.Nodes.Add($"z: {vector.Z}");
			node.Nodes.Add($"w: {vector.W}");
			node.Nodes.Add($"Length: {vector.Length}");

			return node;
		}

		public static TreeNode CreateMatrix(Mat4x4D matrix, string name)
		{
			TreeNode node = Create($"{name} (4x4 Matrix):", name);
			node.Tag = matrix;

			node.Nodes.Add($"Row 1: {matrix.D00} {matrix.D01} {matrix.D02} {matrix.D03}");
			node.Nodes.Add($"Row 2: {matrix.D10} {matrix.D11} {matrix.D12} {matrix.D13}");
			node.Nodes.Add($"Row 3: {matrix.D20} {matrix.D21} {matrix.D22} {matrix.D23}");
			node.Nodes.Add($"Row 4: {matrix.D30} {matrix.D31} {matrix.D32} {matrix.D33}");

			return node;
		}

		public static TreeNode CreateColor(DoubleColor color, string name)
		{
			TreeNode node = Create($"{name} (Color {color}):", name);
			node.Tag = color;

			node.Nodes.Add($"r: {color.R}");
			node.Nodes.Add($"g: {color.G}");
			node.Nodes.Add($"b: {color.B}");
			node.Nodes.Add($"Luminance: {color.Luminance}");

			return node;
		}

		public static TreeNode CreateText(object value, string name)
		{
			TreeNode node = new TreeNode($"{name}: {value}");
			node.Tag = value;
			return node;
		}

		public static void AddProperties(TreeNode node, List<(string name, object value)> properties)
		{
			foreach (var property in properties)
				node.Nodes.Add(Create(property.value, property.name));
		}

		public static TreeNode CreatePrimitive(Primitive primitive, string name)
		{
			if (primitive == null)
				return Create($"{name}: None", name);

			TreeNode node = Create($"{name} ({primitive.GetType().Name}):", name);

			node.Nodes.Add(CreateText(primitive.ID, "ID"));

			node.Tag = primitive;

			AddProperties(node, primitive.Properties);

			return node;
		}

		public static TreeNode CreateHit(Hit hit, string name, Scene scene)
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
			TreeNode node = Create(text, name);
			node.Tag = hit;

			if (hit != default)
			{
				node.Nodes.Add(CreateVector(hit.Position, "Position"));
				node.Nodes.Add(CreateText(hit.Distance, "Distance"));

				if (scene != null)
					node.Nodes.Add(CreatePrimitive(hit.Primitive, "Primitive"));

				node.Nodes.Add(CreateVector(hit.Normal, "Normal"));
				node.Nodes.Add(CreateText(hit.Inside, "Inside"));

#if TRACE
				node.Nodes.Add(Create(hit.DebugText, "Debug"));
#endif
			}

			return node;
		}

		public static TreeNode CreateVolume(IBoundingVolume volume, string name)
		{
			TreeNode node = Create(name);
			AddProperties(node, volume.Properties);
			node.Tag = volume;
			return node;
		}

		public static TreeNode CreateBVHTree(BVH<Primitive> bvhNode, string name, Scene scene)
		{
			string subtitle = bvhNode.IsLeaf ? $"Leaf {bvhNode.LeafID}" : "Branch";

			TreeNode node;

			if (name == null)
				node = Create(subtitle, "Node");
			else
				node = Create($"{name} ({subtitle})", name);

			node.Tag = bvhNode;

			if (!bvhNode.IsLeaf)
			{
				// Add children to the node if we're on a branch
				foreach (BVH<Primitive> child in bvhNode.Children)
					node.Nodes.Add(CreateBVHTree(child, null, scene));
			}

			return node;
		}

		public static TreeNode CreateBVHInfo(BVH<Primitive> bvhNode, string name, Scene scene)
		{
			string subtitle = bvhNode.IsLeaf ? "Leaf" : "Branch";

			TreeNode node;

			if (name == null)
				node = Create(subtitle, "Node");
			else
				node = Create($"{name} ({subtitle})", name);

			node.Tag = bvhNode;

			if (bvhNode.IsLeaf)
				node.Nodes.Add(CreateText(bvhNode.LeafID, "ID"));

			// Add contained primitive to the node if we're on a leaf
			if (bvhNode.IsLeaf)
				node.Nodes.Add(CreatePrimitive(bvhNode.Object, "Object"));

			node.Nodes.Add(CreateVolume(bvhNode.Volume, "Volume"));
			node.Nodes.Add(CreateText(bvhNode.SkipVolume, "Skip volume (parent is same)"));

			return node;
		}

		public static TreeNode CreateBVH(BVH<Primitive> bvhNode, string name)
		{
			return CreateBVHTree(bvhNode, name, null);
		}

		public static TreeNode Create(object value, string name)
		{
			if (value == null)
				return Create("null", name);

			if (value is List<(string name, object value)> properties)
			{
				TreeNode result = Create(name);
				AddProperties(result, properties);
				return result;
			}

			if (value is DoubleColor color)
				return CreateColor(color, name);

			if (value is Vec4D vector)
				return CreateVector(vector, name);

			if (value is Mat4x4D matrix)
				return CreateMatrix(matrix, name);

			if (value is Primitive primitive)
				return CreatePrimitive(primitive, name);

			if (value is Hit hit)
				return CreateHit(hit, name, null);

			if (value is IBoundingVolume volume)
				return CreateVolume(volume, name);

			Type type = value.GetType();
			
			if (type.IsGenericType && typeof(BVH<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
				return (TreeNode) typeof(Nodifier).GetMethod("BVH", BindingFlags.Static).MakeGenericMethod(type.GetGenericArguments()).Invoke(null, new object[]{ value, name });

			return CreateText(value, name);
		}
	}
}
