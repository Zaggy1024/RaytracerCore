using System;
using System.Collections.Generic;
using System.Linq;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Acceleration
{
	public static class KDTree
	{
		private static KDTree<T> Construct<T>(KDTree<T>[] set, int depth) where T : ICenter
		{
			if (set.Length == 1)
				return set[0];

			Axis axis = (Axis)(depth % 3);

			Array.Sort(set, KDTree<T>.GetComparer(axis));

			int half = set.Length / 2;
			KDTree<T>[] left, right;
			left = set[0..half];
			right = set[half..];

			double median = (left[^1].Center[axis] + right[0].Center[axis]) / 2;

#if TRACE
			Util.Assert(!left.Any(n => n.Center[axis] > median), "Left contains an element greater than the median.");
			Util.Assert(!right.Any(n => n.Center[axis] < median), "Right contains an element less than the median.");
#endif

			return new KDTree<T>(axis, median, Construct(left, depth + 1), Construct(right, depth + 1));
		}

		private static KDTree<T> Construct<T>(KDTree<T>[] leaves) where T : ICenter
		{
			KDTree<T> result = Construct(leaves, 0);

#if TRACE && !DEBUG
			// Makeshift unit test to make sure the nearest neighbor algorithm is working correctly
			Random rand = new Random();

			for (int i = 0; i < 25; i++)
			{
				int index = rand.Next(leaves.Length);
				KDTree<T> initial = leaves[index];

				// Fast test
				KDTree<T> fastNode = result.GetNearestNeighbor(initial.Element);

				// Naive, O(N^2) nearest neighbor to compare
				double slowBDist = double.PositiveInfinity;
				KDTree<T> slowNode = default;

				foreach (KDTree<T> elB in leaves)
				{
					if (!initial.Equals(elB))
					{
						double currentDist = (initial.Element.GetCenter() - elB.Element.GetCenter()).SquaredLength;

						if (currentDist < slowBDist)
						{
							slowBDist = currentDist;
							slowNode = elB;
						}
					}
				}

				Util.Assert(result.ContainsElement(fastNode.Element), "Fast nearest neighbor element is not contained in the k-d tree.");
				Util.Assert(result.ContainsElement(slowNode.Element), "Iterative nearest neighbor element is not contained in the k-d tree.");

				double fastDist = -1;
				if (fastNode != null)
					fastDist = (fastNode.Element.GetCenter() - initial.Element.GetCenter()).SquaredLength;

				double slowDist = -1;
				if (slowNode != null)
					slowDist = (slowNode.Element.GetCenter() - initial.Element.GetCenter()).SquaredLength;

				// Determine whether the two returned points have the same distance to the center point.
				Util.AssertNearlyEqual(fastDist, slowDist, 0.0001,
					"k-d tree nearest neighbor algorithm returned an element with a different distance than the iterative algorithm.");
			}
#endif

			return result;
		}

		public static KDTree<T> Construct<T>(IEnumerable<T> objects) where T : ICenter
		{
			return Construct(objects.Select(o => new KDTree<T>(o)).ToArray());
		}

		public static KDTree<T> Construct<T>(List<T> objects) where T : ICenter
		{
			KDTree<T>[] leaves = new KDTree<T>[objects.Count];
			for (int i = 0; i < leaves.Length; i++)
				leaves[i] = new KDTree<T>(objects[i]);
			return Construct(leaves);
		}

		public static KDTree<T> Construct<T>(T[] objects) where T : ICenter
		{
			KDTree<T>[] leaves = new KDTree<T>[objects.Length];
			for (int i = 0; i < leaves.Length; i++)
				leaves[i] = new KDTree<T>(objects[i]);
			return Construct(leaves);
		}
	}

	public class KDTree<T> where T : ICenter
	{
		private static readonly Comparer<KDTree<T>> CompareX = Comparer<KDTree<T>>.Create((a, b) => a.Center[Axis.X].CompareTo(b.Center[Axis.X]));
		private static readonly Comparer<KDTree<T>> CompareY = Comparer<KDTree<T>>.Create((a, b) => a.Center[Axis.Y].CompareTo(b.Center[Axis.Y]));
		private static readonly Comparer<KDTree<T>> CompareZ = Comparer<KDTree<T>>.Create((a, b) => a.Center[Axis.Z].CompareTo(b.Center[Axis.Z]));

		internal static Comparer<KDTree<T>> GetComparer(Axis axis)
		{
			return axis switch
			{
				Axis.X => CompareX,
				Axis.Y => CompareY,
				Axis.Z => CompareZ,
				_ => throw new ArgumentException($"Invalid axis {axis}.", "axis"),
			};
		}

		private bool _Leaf;

		private Axis _Axis;
		private double _Median;
		private KDTree<T> _Left, _Right;

		private T _Element;
		private Vec4D _Center;

		public KDTree(Axis axis, double median, KDTree<T> left, KDTree<T> right)
		{
			SetSplit(axis, median, left, right);
		}

		public KDTree(T element)
		{
			SetElement(element);
		}

		private KDTree()
		{
		}

		private void Reset()
		{
			_Leaf = default;

			_Axis = default;
			_Median = default;
			_Left = default;
			_Right = default;

			_Element = default;
			_Center = default;
		}

		private void SetSplit(Axis axis, double median, KDTree<T> left, KDTree<T> right)
		{
			Reset();
			_Leaf = false;
			_Axis = axis;
			_Median = median;
			_Left = left;
			_Right = right;
		}

		private void SetElement(T element)
		{
			Reset();

			_Leaf = true;
			_Element = element;
			_Center = element.GetCenter();

#if TRACE
			Util.Assert(!_Center.HasNaN(), $"Attempted to create a k-d tree node with an invalid center: ({_Center})");
#endif
		}

		private void CopyFrom(KDTree<T> node)
		{
			_Leaf = node._Leaf;

			_Axis = node._Axis;
			_Median = node._Median;
			_Left = node._Left;
			_Right = node._Right;

			_Element = node._Element;
			_Center = node._Center;
		}

		private void MergeTo(KDTree<T> child)
		{
#if TRACE
			Util.Assert(!IsLeaf, "Cannot merge a leaf.");
			Util.Assert(child.Equals(Left) || child.Equals(Right), "Cannot merge to an element not contained within this node.");
#endif
			CopyFrom(child);
		}

		private KDTree<T> Copy()
		{
			KDTree<T> copy = new KDTree<T>();
			copy.CopyFrom(this);
			return copy;
		}

		private void SplitWith(T element, Axis axis)
		{
			KDTree<T> left = Copy();
			KDTree<T> right = new KDTree<T>(element);
			double leftCoord = left._Center[axis];
			double rightCoord = right._Center[axis];
			double median = (leftCoord + rightCoord) / 2;

			if (leftCoord > rightCoord)
			{
				KDTree<T> temp = left;
				left = right;
				right = temp;
			}

			SetSplit(axis, median, left, right);
		}

		public Axis Axis => _Axis;
		public double Median => _Median;
		public KDTree<T> Left => _Left;
		public KDTree<T> Right => _Right;

		public bool IsLeaf => _Leaf;
		public T Element => _Element;
		public Vec4D Center => _Center;

		public int LeafCount
		{
			get
			{
				if (IsLeaf)
					return 1;

				return _Left.LeafCount + _Right.LeafCount;
			}
		}

		private bool ContainsElement(T element, Vec4D point)
		{
			if (IsLeaf)
				return element.Equals(Element);

			double coord = point[Axis];

			if (coord <= Median && Left.ContainsElement(element, point))
				return true;

			if (coord >= Median && Right.ContainsElement(element, point))
				return true;

			return false;
		}

		public bool ContainsElement(T element)
		{
			return ContainsElement(element, element.GetCenter());
		}

		private void GetNearestNeighbor(T element, Vec4D point, ref KDTree<T> bestNode, ref double bestDist)
		{
			if (IsLeaf)
			{
				if (!element.Equals(_Element))
				{
					double currentDist = (_Center - point).SquaredLength;

					if (currentDist < bestDist)
					{
						bestNode = this;
						bestDist = currentDist;
					}
				}

				return;
			}

			double coord = point[_Axis];
			KDTree<T> primary;
			KDTree<T> secondary;

			if (coord <= _Median)
			{
				primary = _Left;
				secondary = _Right;
			}
			else
			{
				primary = _Right;
				secondary = _Left;
			}

			primary.GetNearestNeighbor(element, point, ref bestNode, ref bestDist);

			// If secondary node is closer than the best distance we have, step into that as well
			double boundDist = Math.Abs(coord - _Median);
			boundDist *= boundDist;

			if (boundDist < bestDist)
				secondary.GetNearestNeighbor(element, point, ref bestNode, ref bestDist);
		}

		public KDTree<T> GetNearestNeighbor(T element)
		{
			KDTree<T> node = null;
			double distance = double.PositiveInfinity;
			GetNearestNeighbor(element, element.GetCenter(), ref node, ref distance);
			return node;
		}

		private bool GetAddress(T element, Vec4D point, in Stack<BinaryTreeAddress.Side> address, out T finalItem)
		{
			if (IsLeaf)
			{
				finalItem = _Element;
				return element.Equals(_Element);
			}

			double pK = point[_Axis];

			if (pK == _Median)
			{
				address.Push(BinaryTreeAddress.Side.Left);
				if (_Left.GetAddress(element, point, address, out finalItem))
					return true;
				address.Pop();
				address.Push(BinaryTreeAddress.Side.Right);
				return _Right.GetAddress(element, point, address, out finalItem);
			}
			else if (pK < _Median)
			{
				address.Push(BinaryTreeAddress.Side.Left);
				return _Left.GetAddress(element, point, address, out finalItem);
			}
			else
			{
				address.Push(BinaryTreeAddress.Side.Right);
				return _Right.GetAddress(element, point, address, out finalItem);
			}
		}

		public BinaryTreeAddress<T> GetAddress(T element)
		{
			Stack<BinaryTreeAddress.Side> address = new Stack<BinaryTreeAddress.Side>();
			bool found = GetAddress(element, element.GetCenter(), address, out T finalItem);
			return new BinaryTreeAddress<T>(address.ToArray(), found, finalItem);
		}

		private bool GetParent(T element, Vec4D point, ref KDTree<T> parent)
		{
			if (IsLeaf)
				return element.Equals(_Element);

			KDTree<T> grandparent = parent;
			double pK = point[_Axis];

			if (pK <= _Median)
			{
				parent = this;
				if (Left.GetParent(element, point, ref parent))
					return true;
			}

			if (pK >= _Median)
			{
				parent = this;
				if (Right.GetParent(element, point, ref parent))
					return true;
			}

			parent = grandparent;

			return false;
		}

		/// <summary>
		/// Find an exact match of <paramref name="element"/>, then return its parent node.
		/// </summary>
		/// <param name="element">The element to find in the tree.</param>
		/// <returns>The parent node, or null if the object was not found.</returns>
		public KDTree<T> GetParent(T element)
		{
			KDTree<T> parent = this;
			GetParent(element, element.GetCenter(), ref parent);
			return parent;
		}

		/// <summary>
		/// Removes the node containing <paramref name="element"/> from the tree.
		/// </summary>
		/// <param name="element">The object to find and remove from the tree.</param>
		public void Remove(T element)
		{
			KDTree<T> parent = GetParent(element);
			parent.MergeTo(element.Equals(parent._Left._Element) ? parent._Right : parent._Left);
		}

		private KDTree<T> GetClosestLeaf(T element, Vec4D point, ref KDTree<T> parent)
		{
			if (IsLeaf)
				return this;

			double pK = point[_Axis];

			if (pK <= _Median)
			{
				parent = this;
				return Left.GetClosestLeaf(element, point, ref parent);
			}
			else
			{
				parent = this;
				return Right.GetClosestLeaf(element, point, ref parent);
			}
		}

		/// <summary>
		/// Find an exact match of <paramref name="element"/>, then return its parent node.
		/// </summary>
		/// <param name="element">The element to find in the tree.</param>
		/// <returns>The parent node, or null if the object was not found.</returns>
		public KDTree<T> GetClosestLeaf(T element, out KDTree<T> parent)
		{
			parent = this;
			return GetClosestLeaf(element, element.GetCenter(), ref parent);
		}

		/// <summary>
		/// Add a new node to the tree, splitting a leaf to make room.
		/// </summary>
		/// <param name="element">The object to add to the tree.</param>
		public void Add(T element)
		{
			KDTree<T> node = GetClosestLeaf(element, out KDTree<T> parent);
			// Get the axis for this depth to initialize new node
			Axis axis = (Axis)(((int)parent._Axis + 1) % 3);
			node.SplitWith(element, axis);
		}

		/// <summary>
		/// Creates a list of the leaf nodes contained within this tree or branch.
		/// </summary>
		public List<KDTree<T>> Flatten()
		{
			if (IsLeaf)
				return new List<KDTree<T>>() { this };

			Stack<(KDTree<T> node, int index)> stack = new Stack<(KDTree<T> node, int index)>();
			stack.Push((this, 0));

			List<KDTree<T>> list = new List<KDTree<T>>();

			while (true)
			{
				var (node, index) = stack.Peek();

				if (node.IsLeaf)
				{
#if TRACE
					Util.Assert(!list.Contains(node), "Nodes must be unique.");
#endif

					// Yield a node
					list.Add(node);
					stack.Pop();
					(KDTree<T> node, int index) next;

					// Then step to the next neighbor
					while (true)
					{
						next = stack.Pop();
						next.index++;

						if (next.index < 2)
							break;
						else if (stack.Count == 0)
							return list;
					}

					stack.Push(next);
				}
				else
				{
					// Step into child
					switch (index)
					{
						case 0:
							stack.Push((node._Left, 0));
							break;
						case 1:
							stack.Push((node._Right, 0));
							break;
						default:
							throw new IndexOutOfRangeException($"Index {index} exceeded range of 0-1 while flattening a BVH.");
					}
				}
			}
		}

		public override bool Equals(object other)
		{
			if (other is KDTree<T> otherNode)
			{
				if (this == otherNode)
					return true;

				if (IsLeaf)
				{
					if (!otherNode.IsLeaf)
						return false;

					return _Element.Equals(otherNode._Element);
				}
				else if (otherNode.IsLeaf)
				{
					return false;
				}

				if (!(_Left.Equals(otherNode._Left) || _Left.Equals(otherNode._Right)))
					return false;

				if (!(_Right.Equals(otherNode._Left) || _Right.Equals(otherNode._Right)))
					return false;

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			if (IsLeaf)
				return _Element.GetHashCode();
			else
				return _Left.GetHashCode() ^ _Right.GetHashCode();
		}

		public override string ToString()
		{
			if (IsLeaf)
				return $"k-d Leaf {{{_Element}}}";

			return $"k-d Branch ({LeafCount} Leaves)";
		}
	}
}
