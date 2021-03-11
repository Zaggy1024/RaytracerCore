using System;
using System.Collections.Generic;
using System.Linq;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Acceleration
{
	public static class BVH
	{
		private static IEnumerable<BVH<T>> Pairs<T>(IList<BVH<T>> nodes) where T : class, IBoundedObject
		{
			foreach (BVH<T> a in nodes)
			{
				foreach (BVH<T> b in nodes)
				{
					if (a != b)
					{
						Util.Assert(!a.Equals(b), "Two nodes in a pair were equal.");
						yield return new BVH<T>(a, b);
					}
				}
			}
		}

		private static List<BVH<T>> Condense<T>(List<BVH<T>> nodes) where T : class, IBoundedObject
		{
			Dictionary<BVH<T>, int> remaining = new Dictionary<BVH<T>, int>(nodes.Count);
			for (int i = 0; i < nodes.Count; i++)
				remaining[nodes[i]] = i;

			IEnumerable<BVH<T>> sorted = Pairs(nodes).OrderBy((n) => n.Cost);
			List<BVH<T>> result = new List<BVH<T>>();

			foreach (BVH<T> node in sorted)
			{
				// Do not pair up any nodes containing already-paired children
				if (!remaining.ContainsKey(node.Left) ||
					!remaining.ContainsKey(node.Right))
					continue;

				result.Add(node);

				remaining.Remove(node.Left);
				remaining.Remove(node.Right);
			}

			result.AddRange(remaining.OrderBy((kvp) => kvp.Value).Select((kvp) => kvp.Key));

			return result;
		}

		public static BVH<T> Construct<T>(List<T> objects) where T : class, IBoundedObject
		{
			List<BVH<T>> intermediate = objects.Select((o) => new BVH<T>(o)).ToList();

			while (intermediate.Count > 1)
			{
				intermediate = Condense(intermediate);
			}

			Util.Assert(intermediate.Count == 1, $"Constructing a BVH resulted in wrong count after condensing: {intermediate.Count}");

			return intermediate[0];
		}
	}

	public class BVH<T> where T : class, IBoundedObject
	{
		public readonly T Object;
		public readonly BVH<T> Left;
		public readonly BVH<T> Right;

		public readonly IBoundingVolume Volume;

		private double _Cost = -1;
		private int _HashCode = -1;

		public BVH(T obj)
		{
			Object = obj;
			Left = Right = null;
			Volume = AABB.CreateFromBounded(obj);
		}

		public BVH(BVH<T> left, BVH<T> right)
		{
			Object = default;
			Left = left;
			Right = right;
			Volume = AABB.Combine(left.Volume, right.Volume);
		}

		public bool IsLeaf => Object != default;

		public BVH<T>[] Children => new BVH<T>[] { Left, Right };

		/// <summary>
		/// Intersect a ray with all bounding volumes, adding all intersections to the provided list.
		/// </summary>
		/// <param name="ray">The ray to use for intersections.</param>
		/// <param name="list">A reference to a list to add intersections to.</param>
		public void IntersectAll(Ray ray, ref List<BoundingIntersection<T>> list)
		{
			(var near, var far) = Volume.Intersect(ray);

			// If our ray doesn't intersect the volume (far < 0 or NaN), skip
			if (!(far >= 0))
				return;

			if (IsLeaf)
			{
				// If we're on a leaf of the tree, add the object
				list.Add(new BoundingIntersection<T>(this, near, far));
				return;
			}

			// Otherwise, begin intersecting the children
			Left.IntersectAll(ray, ref list);
			Right.IntersectAll(ray, ref list);
		}

		/// <summary>
		/// Intersect a ray with all bounding volumes, returning all intersections in a list.
		/// </summary>
		/// <param name="ray">The ray to use for intersections.</param>
		/// <returns>A list of intersections, sorted by distance.</returns>
		public List<BoundingIntersection<T>> IntersectAll(Ray ray)
		{
			List<BoundingIntersection<T>> result = new List<BoundingIntersection<T>>();
			IntersectAll(ray, ref result);
			result.Sort((v1, v2) => v1.Near.CompareTo(v2.Near));
			return result;
		}

		/// <summary>
		/// Gets the average distance between the children of this node. Used to calculate the efficiency of groupings of nodes.
		/// </summary>
		public double Cost
		{
			get
			{
				if (IsLeaf)
					throw new Exception("Cannot get the cost for a BVH leaf node.");

				/*if (_Cost == -1)
				{
					double total = 0;
					int count = 0;

					foreach (BVH<T> a in Children)
					{
						foreach (BVH<T> b in Children)
						{
							if (a != b)
							{
								count++;
								total += (a.Volume.GetCenter() - b.Volume.GetCenter()).Length;
							}
						}
					}

					_Cost = total / count;
				}*/

				if (_Cost == -1)
					_Cost = Volume.GetVolume();

				return _Cost;
			}
		}

		public List<BVH<T>> Flatten()
		{
			if (IsLeaf)
				return new List<BVH<T>>() { this };

			Stack<(BVH<T> node, int index)> stack = new Stack<(BVH<T> node, int index)>();
			stack.Push((this, 0));

			List<BVH<T>> list = new List<BVH<T>>();

			while (true)
			{
				var (node, index) = stack.Peek();

				if (node.IsLeaf)
				{
					Util.Assert(!list.Contains(node), "Nodes must be unique.");

					// Yield a node
					list.Add(node);
					stack.Pop();
					(BVH<T> node, int index) next;

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
							stack.Push((node.Left, 0));
							break;
						case 1:
							stack.Push((node.Right, 0));
							break;
						default:
							throw new IndexOutOfRangeException($"Index {index} exceeded range of 0-1 while flattening a BVH.");
					}
				}
			}
		}

		public int LeafCount
		{
			get
			{
				if (IsLeaf)
					return 1;

				return Left.LeafCount + Right.LeafCount;
			}
		}

		public override bool Equals(object other)
		{
			if (other is BVH<T> otherNode)
			{
				if (this == otherNode)
					return true;

				if (IsLeaf)
				{
					if (!otherNode.IsLeaf)
						return false;

					return Object == otherNode.Object;
				}
				else if (otherNode.IsLeaf)
				{
					return false;
				}

				if (!(Left.Equals(otherNode.Left) || Left.Equals(otherNode.Right)))
					return false;

				if (!(Right.Equals(otherNode.Left) || Right.Equals(otherNode.Right)))
					return false;

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			// Cache the hashcode to avoid recursion when indexing
			if (_HashCode == -1)
			{
				if (IsLeaf)
					_HashCode = Object.GetHashCode();
				else
					_HashCode = Left.GetHashCode() ^ Right.GetHashCode();
			}

			return _HashCode;
		}

		public override string ToString()
		{
			if (IsLeaf)
				return $"Leaf {{{Object}}}";

			return $"Branch ({LeafCount} Leaves)";
		}
	}
}
