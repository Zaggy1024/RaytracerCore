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
				if (node.Children.Any((n) => !remaining.ContainsKey(n)))
					continue;

				result.Add(node);

				foreach (BVH<T> child in node.Children)
					remaining.Remove(child);
			}

			result.AddRange(remaining.OrderBy((kvp) => kvp.Value).Select((kvp) => kvp.Key));

			return result;
		}

		public static BVH<T> Construct<T>(List<T> objects) where T : class, IBoundedObject
		{
			List<BVH<T>> intermediate = objects.Select((o) => new BVH<T>(o)).ToList();
			BVH<T> single;

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
		public readonly BVH<T>[] Children;

		public readonly IBoundingVolume Volume;

		private double _Cost = -1;
		private int _HashCode = -1;

		public BVH(T obj)
		{
			Object = obj;
			Children = default;
			Volume = AABB.CreateFromBounded(obj);
		}

		public BVH(params BVH<T>[] children)
		{
			Util.Assert(children.Length > 0, "Non-leaf BVH node cannot be constructed with no children.");

			Object = default;
			Children = children;
			Volume = children[0].Volume;

			foreach (BVH<T> child in children.Skip(1))
				Volume = AABB.Combine(Volume, child.Volume);
		}

		public bool IsLeaf => Object != default;

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
			foreach (BVH<T> child in Children)
				child.IntersectAll(ray, ref list);
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
			if (Children.Length == 0)
				return new List<BVH<T>>();

			Stack<(BVH<T> node, int index)> stack = new Stack<(BVH<T> node, int index)>();
			stack.Push((this, 0));

			List<BVH<T>> list = new List<BVH<T>>();

			while (true)
			{
				var current = stack.Peek();

				if (current.node.IsLeaf)
				{
					Util.Assert(!list.Contains(current.node), "Nodes must be unique.");

					// Yield a node
					list.Add(current.node);
					stack.Pop();
					(BVH<T> node, int index) next;

					// Then step to the next neighbor
					while (true)
					{
						next = stack.Pop();
						next.index++;

						if (next.index < next.node.Children.Length)
							break;
						else if (stack.Count == 0)
							return list;
					}

					stack.Push(next);
				}
				else
				{
					// Step into child
					stack.Push((current.node.Children[current.index], 0));
				}
			}
		}

		public int LeafCount
		{
			get
			{
				if (IsLeaf)
					return 1;

				int count = 0;

				foreach (BVH<T> child in Children)
					count += child.LeafCount;

				return count;
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

				foreach (BVH<T> leftChild in Children)
				{
					if (!otherNode.Children.Contains(leftChild))
						return false;
				}

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			if (_HashCode == -1)
			{
				if (IsLeaf)
				{
					_HashCode = Object.GetHashCode();
				}
				else
				{
					_HashCode = 0;

					foreach (BVH<T> child in Children)
						_HashCode ^= child.GetHashCode();
				}
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
