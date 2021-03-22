using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RaytracerCore.Raytracing.Acceleration
{
	public class MinQueue<T> : IEnumerable<T>
	{
		private class Node
		{
			public bool IsLeaf;
			public T Value;
			public Node Left;
			public Node Right;

			public Node(T value)
			{
				IsLeaf = true;
				Value = value;
			}

			public Node(Node left, Node right, T median)
			{
				IsLeaf = false;
				Value = median;
				Left = left;
				Right = right;
			}

			public Node(T left, T right) : this(new Node(left), new Node(right), left)
			{
			}

			public static Node CreateNode(T[] values, Comparer<T> comparer)
			{
				if (values.Length == 1)
					return new Node(values[0]);

				int half = values.Length / 2;

				if (comparer.Compare(values[0], values[^1]) != 0)
				{
					T median = values[half];

					// Find the closest change in sorting value to split around.
					int offset = 1;
					while (half + offset < values.Length)
					{
						int index = half + offset;
						if (index >= values.Length)
							break;
						if (comparer.Compare(values[index], median) != 0)
						{
							half = index;
							break;
						}

						index = half - offset;
						if (index >= 0 && comparer.Compare(values[index], median) != 0)
						{
							half = index + 1;
							break;
						}
						offset++;
					}
				}

				T[] left = values[..half];
				T[] right = values[half..];
				return new Node(CreateNode(left, comparer), CreateNode(right, comparer), values[half - 1]);
			}

			public void SplitWith(T other, bool flip)
			{
				Util.Assert(IsLeaf, "Cannot split a non-leaf.");

				IsLeaf = false;
				
				if (flip)
				{
					Left = new Node(other);
					Right = new Node(Value);
					Value = other;
				}
				else
				{
					Left = new Node(Value);
					Right = new Node(other);
				}
			}

			public void JoinRight()
			{
				Util.Assert(!IsLeaf, "Cannot join a leaf.");

				IsLeaf = Right.IsLeaf;
				Value = Right.Value;
				Left = Right.Left;
				Right = Right.Right;
			}
		}

		private readonly Comparer<T> Comparer;

		private Node Root;
		private Node MinNode;

		public MinQueue(Comparer<T> comparer)
		{
			Comparer = comparer;
		}

		private MinQueue(T[] values, Comparer<T> comparer) : this(comparer)
		{
			if (values.Length == 0)
				return;

			if (values.Length <= 20)
				Util.InsertSort(values, comparer);
			else
				Util.MergeSort(values, comparer);

			Root = Node.CreateNode(values, comparer);
		}

		public MinQueue(List<T> values, Comparer<T> comparer) : this(values.ToArray(), comparer)
		{
		}

		public MinQueue(IEnumerable<T> values, Comparer<T> comparer) : this(values.ToArray(), comparer)
		{
		}

		public bool IsEmpty => Root == null;

		private Node FindMin(Node node)
		{
			if (node.IsLeaf)
				return node;
			return FindMin(node.Left);
		}

		public T Min
		{
			get
			{
				if (IsEmpty)
					throw new InvalidOperationException("The queue is empty.");

				if (MinNode != null)
				{
					return MinNode.Value;
				}

				return (MinNode = FindMin(Root)).Value;
			}
		}

		[Conditional("DEBUG")]
		private void Verify()
		{
			/*Util.Assert(MinNode == FindMin(Root), "Current cached minimum did not match the actual minimum.");
			T fastMin = MinNode.Value;
			T slowMin = this.Aggregate((a, b) => Comparer.Compare(a, b) <= 0 ? a : b);
			Util.Assert(Comparer.Compare(fastMin, slowMin) == 0, "Minimum was incorrect.");

			Util.Assert(this.SequenceEqual(this.OrderBy(v => v, Comparer)), "Queue is in the incorrect order.");*/
		}

		private Node FindNode(Node node, T value)
		{
			if (node.IsLeaf)
				return node;

			if (Comparer.Compare(value, node.Value) <= 0)
				return FindNode(node.Left, value);
			else
				return FindNode(node.Right, value);
		}

		public void Add(T value)
		{
			if (Root == null)
			{
				Root = new Node(value);
				MinNode = Root;
			}
			else
			{
				Node node = FindNode(Root, value);
				node.SplitWith(value, Comparer.Compare(value, node.Value) <= 0);

				if (node == MinNode)
					MinNode = node.Left;
			}

			Verify();
		}

		public void RemoveMin()
		{
			if (Root.IsLeaf)
			{
				Root = null;
			}
			else
			{
				Node parent = Root;
				Node current;

				while (true)
				{
					current = parent.Left;
					if (current.IsLeaf)
						break;
					parent = current;
				}

				parent.JoinRight();
				MinNode = FindMin(parent);
			}

			Verify();
		}

		private class StackPos
		{
			public Node Node;
			public int Index;

			public StackPos(Node node, int index)
			{
				Node = node;
				Index = index;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			if (Root == null)
				yield break;

			Stack<StackPos> stack = new Stack<StackPos>();
			stack.Push(new StackPos(Root, 0));

			while (stack.Count > 0)
			{
				StackPos current = stack.Peek();
				Node child = current.Index == 0 ? current.Node.Left : current.Node.Right;

				if (child.IsLeaf)
				{
					yield return child.Value;

					while (++current.Index >= 2)
					{
						stack.Pop();
						if (stack.Count == 0)
							break;

						current = stack.Peek();
					}
				}
				else
				{
					stack.Push(new StackPos(child, 0));
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (T value in this)
				yield return value;
		}

		private int GetCount(Node node)
		{
			if (node.IsLeaf)
				return 1;

			return GetCount(node.Left) + GetCount(node.Right);
		}

		public int Count
		{
			get
			{
				if (Root == null)
					return 0;

				return GetCount(Root);
			}
		}

		public override string ToString()
		{
			return $"Queue Count = {this.Count()}";
		}
	}
}
