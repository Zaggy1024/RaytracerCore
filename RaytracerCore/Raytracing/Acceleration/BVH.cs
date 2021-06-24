using System;
using System.Collections.Generic;
using System.Linq;

using RaytracerCore.Vectors;
using static RaytracerCore.Raytracing.Acceleration.BVH;

namespace RaytracerCore.Raytracing.Acceleration
{
	public static class BVH
	{
		public static double GetCost(IBoundingVolume parent)
		{
			return parent.GetSurfaceArea();
			//return volume.GetVolume();
		}

		public static double GetCost<T>(BVH<T> left, BVH<T> right) where T : IBoundedObject
		{
			return GetCost(AABB.Combine(left.Volume, right.Volume));
		}

		/// <summary>
		/// Creates a parent of the two provided nodes.
		/// This will change values on the child to make the BVH structure as efficient as possible,
		/// so only use this function on a final pair.
		/// </summary>
		/// <param name="left">The left child node.</param>
		/// <param name="right">The right child node.</param>
		/// <returns>A new node which will be the parent of the provided nodes.</returns>
		public static BVH<T> CreateParent<T>(BVH<T> left, BVH<T> right) where T : IBoundedObject
		{
			BVH<T> parent = new BVH<T>(left, right);
			MakeParent(parent);
			return parent;
		}

		/// <summary>
		/// Finalizes making a BVH node a parent to its children.
		/// This will change values on the child to make the BVH structure as efficient as possible,
		/// so only use this function on a final pair.
		/// </summary>
		/// <param name="left">The node to finalize as a parent.</param>
		public static void MakeParent<T>(BVH<T> parent) where T : IBoundedObject
		{
			parent.Left.SkipVolume = parent.Left.Volume.Equals(parent.Volume);
			parent.Right.SkipVolume = parent.Right.Volume.Equals(parent.Volume);
		}

		private static BVH<T> ConstructLocalAgglom<T>(List<T> objects) where T : IBoundedObject
		{
			// See "Fast Agglomerative Clustering for Rendering": https://www.cs.cornell.edu/~kb/publications/IRT08.pdf
			List<BVH<T>> leafList = objects.Select((o, i) => new BVH<T>(o, i)).ToList();

			KDTree<BVH<T>> tree = KDTree.Construct(leafList);

			BVH<T> a = leafList[0];
			BVH<T> b = tree.GetNearestNeighbor(a).Element;

			while (true)
			{
				BVH<T> c = tree.GetNearestNeighbor(b).Element;

				if (a == c)
				{
					tree.Remove(a);
					Util.Assert(!tree.ContainsElement(a), "test");
					a = CreateParent(a, b);

					// If the tree is a leaf, we're removing the last elements, exit early.
					if (tree.IsLeaf)
						return a;

					tree.Remove(b);
					Util.Assert(!tree.ContainsElement(b), "test");
					tree.Add(a);
					Util.Assert(tree.ContainsElement(a), "test");

					b = tree.GetNearestNeighbor(a).Element;
				}
				else
				{
					a = b;
					b = c;
				}
			}
		}

		private static BVH<T> ConstructHeapAgglom<T>(List<T> objects) where T : IBoundedObject
		{
			BVH<T>[] nodes = new BVH<T>[objects.Count];

			// Make array of leaves to construct the k-d tree
			for (int i = 0; i < nodes.Length; i++)
				nodes[i] = new BVH<T>(objects[i], i);

			KDTree<BVH<T>> tree = KDTree.Construct(nodes);

			// Prepare priority queue to get the cheapest pairs
			Comparer<BVH<T>> pairComparer = Comparer<BVH<T>>.Create((a, b) =>
			{
				if (a.Equals(b))
					return 0;

				int comp = a.Cost.CompareTo(b.Cost);
				if (comp != 0)
					return comp;

				// Break ties by preferring  more leaves first to flatten the tree.
				comp = b.ChildLeaves.CompareTo(a.ChildLeaves);
				if (comp != 0)
					return comp;

				return 0;
			});

			// Make list of nearest pairs
			for (int i = 0; i < nodes.Length; i++)
			{
				BVH<T> node = nodes[i];
				nodes[i] = new BVH<T>(node, tree.GetNearestNeighbor(node).Element);
			}

			/*MinQueue<BVH<T>> cheapPairs = new MinQueue<BVH<T>>(nodes, pairComparer);

			while (true)
			{
				// Get the next cheapest pair to make
				BVH<T> cheapest = cheapPairs.Min;
				cheapPairs.RemoveMin();

				// If we've already paired this node, leave it removed.
				if (!tree.ContainsElement(cheapest.Left))
					continue;

				// If we've paired the left element but not the right, find the new nearest neighbor and resort.
				if (!tree.ContainsElement(cheapest.Right))
				{
					cheapPairs.Add(new BVH<T>(cheapest.Left, tree.GetNearestNeighbor(cheapest.Left).Element));
					cheapPairs = new MinQueue<BVH<T>>(cheapPairs, pairComparer);
					continue;
				}

				// Finalize parentizing this pair, so that resulting BVH is more efficient.
				MakeParent(cheapest);

				tree.Remove(cheapest.Left);

				// If the tree is a leaf, we're removing the last elements, exit early.
				if (tree.IsLeaf)
					return cheapest;

				tree.Remove(cheapest.Right);
				tree.Add(cheapest);

				cheapPairs.Add(new BVH<T>(cheapest, tree.GetNearestNeighbor(cheapest).Element));
			}*/

			Heap<BVH<T>> cheapPairs = new Heap<BVH<T>>(nodes, pairComparer);

			while (true)
			{
				// Get the next cheapest pair to make
				BVH<T> cheapest = cheapPairs.Extract();

				// If we've already paired this node, leave it removed.
				if (!tree.ContainsElement(cheapest.Left))
					continue;

				// If we've paired the left element but not the right, find the new nearest neighbor and resort.
				if (!tree.ContainsElement(cheapest.Right))
				{
					cheapPairs.Add(new BVH<T>(cheapest.Left, tree.GetNearestNeighbor(cheapest.Left).Element));
					continue;
				}

				// Finalize parentizing this pair, so that resulting BVH is more efficient.
				MakeParent(cheapest);

				tree.Remove(cheapest.Left);

				// If the tree is a leaf, we're removing the last elements, exit early.
				if (tree.IsLeaf)
					return cheapest;

				tree.Remove(cheapest.Right);
				tree.Add(cheapest);

				cheapPairs.Add(new BVH<T>(cheapest, tree.GetNearestNeighbor(cheapest).Element));
			}
		}

		public static BVH<T> Construct<T>(List<T> objects) where T : IBoundedObject
		{
			if (objects.Count > 200000)
				return ConstructLocalAgglom(objects);

			if (objects.Count > 20)
				return ConstructHeapAgglom(objects);

			HashSet<BVH<T>> intermediate = objects.Select((o, i) => new BVH<T>(o, i)).ToHashSet();

			while (intermediate.Count > 1)
			{
				(BVH<T>, BVH<T>) best = default;
				double bestCost = double.PositiveInfinity;

				foreach (BVH<T> a in intermediate)
				{
					foreach (BVH<T> b in intermediate)
					{
						if (a != b)
						{
							(BVH<T>, BVH<T>) pair = (a, b);
							double cost = GetCost(a, b);

							// If this pair is a better option than any previously found,
							// or break a tie by preferring to pair leaves to keep the tree shallow.
							if (best == default
								|| cost < bestCost
								|| (cost == bestCost && a.IsLeaf && b.IsLeaf))
							{
								best = pair;
								bestCost = cost;
							}
						}
					}
				}

				intermediate.Remove(best.Item1);
				intermediate.Remove(best.Item2);
				intermediate.Add(CreateParent(best.Item1, best.Item2));
			}

			return intermediate.First();
		}
	}

	public class BVH<T> : ICenter where T : IBoundedObject
	{
		private static readonly Comparer<BoundingIntersection<T>> IntersectionSorter = Comparer<BoundingIntersection<T>>.Create((a, b) => a.Near.CompareTo(b.Near));

		public readonly bool IsLeaf;

		public readonly T Object;
		public readonly int LeafID;

		public readonly BVH<T> Left, Right;

		public readonly AABB Volume;

		public bool SkipVolume { get; internal set; }
		private double _Cost = -1;
		private int _HashCode = -1;

		public BVH(T obj, int id)
		{
			IsLeaf = true;
			Object = obj;
			LeafID = id;
			Volume = AABB.CreateFromBounded(obj);

			Left = Right = null;
		}

		public BVH(T obj) : this(obj, -1)
		{
		}

		public BVH(BVH<T> left, BVH<T> right)
		{
			IsLeaf = false;
			Left = left;
			Right = right;
			Volume = AABB.Combine(left.Volume, right.Volume);

			LeafID = -1;
			Object = default;
		}

		public BVH<T>[] Children => new BVH<T>[] { Left, Right };

		public Vec4D GetCenter()
		{
			if (IsLeaf)
				return Object.GetCenter();
			return Volume.GetCenter();
		}

		/// <summary>
		/// Intersect a ray with all leaves' bounding volumes, adding all intersections to the provided list.
		/// </summary>
		/// <param name="ray">The ray to use for intersections.</param>
		/// <param name="list">A reference to a list to add intersections to.</param>
		public void IntersectLeaves(Ray ray, in ICollection<BoundingIntersection<T>> list, (double near, double far) distances)
		{
			if (!SkipVolume)
			{
				distances = Volume.Intersect(ray);

				// If our ray doesn't intersect the volume (far < 0 or NaN), skip
				if (!(distances.far >= 0))
					return;
			}

			// If we're on a leaf of the tree, add the object
			if (IsLeaf)
			{
				list.Add(new BoundingIntersection<T>(this, distances.near, distances.far));
				return;
			}

			// Otherwise, begin intersecting the children
			Left.IntersectLeaves(ray, list, distances);
			Right.IntersectLeaves(ray, list, distances);
		}

		/// <summary>
		/// Intersect a ray with all leaves' bounding volumes, returning all intersections in a list.
		/// </summary>
		/// <param name="ray">The ray to use for intersections.</param>
		/// <returns>A list of intersections, sorted by distance.</returns>
		public IEnumerable<BoundingIntersection<T>> IntersectLeaves(Ray ray)
		{
			// Initialize the list with an arbitrary number as a guess of the most hits per ray we should usually expect
			List<BoundingIntersection<T>> result = new List<BoundingIntersection<T>>(5);
			IntersectLeaves(ray, result, default);
			// Use a stable sort for more consistency, slightly slower than List.Sort (unstable)
			Util.InsertSort(result, IntersectionSorter);
			return result;
		}

		private int GetMaxDepth(int depth)
		{
			if (IsLeaf)
				return depth;

			depth++;
			return Math.Max(Left.GetMaxDepth(depth), Right.GetMaxDepth(depth));
		}

		public int GetMaxDepth()
		{
			return GetMaxDepth(0);
		}

		/// <summary>
		/// Gets the number of branches or leaves intersecting with the provided ray.
		/// </summary>
		/// <param name="ray">The ray to use for intersections.</param>
		/// <returns>The count of intersecting tree nodes.</returns>
		public int GetIntersectionCount(Ray ray)
		{
			bool hit = Volume.Intersect(ray).far >= 0;

			if (!hit)
				return 0;

			if (IsLeaf)
				return 1;

			return 1 + Left.GetIntersectionCount(ray) + Right.GetIntersectionCount(ray);
		}

		/// <summary>
		/// Gets a heuristic to determine the efficiency of grouping this node's children.
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
					_Cost = GetCost(Volume);

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

#if TRACE
			HashSet<BVH<T>> added = new HashSet<BVH<T>>();
#endif

			while (true)
			{
				var (node, index) = stack.Peek();

				if (node.IsLeaf)
				{
#if TRACE
					Util.Assert(!added.Contains(node), "Nodes must be unique.");
					added.Add(node);
#endif

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

		public int TotalLeaves
		{
			get
			{
				if (IsLeaf)
					return 1;

				return Left.TotalLeaves + Right.TotalLeaves;
			}
		}

		public int ChildLeaves => Convert.ToInt32(Left.IsLeaf) + Convert.ToInt32(Right.IsLeaf);

		public override bool Equals(object other)
		{
			if (other == null)
				return false;

			if (this == other)
				return true;

			// Early exit based on cached hashcode
			if (GetHashCode() != other.GetHashCode())
				return false;

			// Handle hash collisions as a last resort
			if (other is BVH<T> otherNode)
			{
				if (IsLeaf)
				{
					if (!otherNode.IsLeaf)
						return false;

					return Object.Equals(otherNode.Object);
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
				return $"BVH Leaf {{{Object}}}";

			return $"BVH Branch ({TotalLeaves} Leaves)";
		}
	}
}
