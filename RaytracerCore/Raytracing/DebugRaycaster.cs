using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Linq;
using System.Windows.Forms;

using RaytracerCore.Vectors;
using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Raytracing.Acceleration;
using RaytracerCore.Raytracing.Cameras;
using System.ComponentModel;
using RaytracerCore.Raytracing.Objects;

namespace RaytracerCore.Raytracing
{
	public class DebugRaycaster
	{
		public interface IIntersector
		{
			public double Intersect(Ray ray);
			public int ID { get; }
		}

		public class PrimitiveIntersector : IIntersector
		{
			private readonly Primitive Primitive;

			public PrimitiveIntersector(Primitive primitive)
			{
				Primitive = primitive;
			}

			public double Intersect(Ray ray)
			{
				Hit hit = Primitive.RayTrace(ray, null);

				if (hit != default)
					return hit.Distance;

				return double.NaN;
			}

			public int ID => Math.Max(Primitive.ID, 0);
		}

		public class BVHIntersector : IIntersector
		{
			private readonly BVH<Primitive> Node;

			public BVHIntersector(BVH<Primitive> node)
			{
				Node = node;
			}

			public double Intersect(Ray ray)
			{
				var (near, far) = Node.Volume.Intersect(ray);

				if (near >= 0)
					return near;

				return far;
			}

			public int ID
			{
				get
				{
					int id = Node.LeafID;
					if (id < 0)
						return Node.GetHashCode();
					return id;
				}
			}
		}

		protected static readonly Color[] ColorRotation = new Color[]
		{
			Color.Red,
			Color.Green,
			Color.Blue,
			Color.Yellow,
			Color.Magenta,
			Color.Cyan,
			Color.DarkGray
		};

		public enum DisplayMode
		{
			[Description("Primitives")]
			Primitives,
			[Description("Bounding Volumes")]
			BoundingVolumes,
			[Description("Selection")]
			Selection
		}

		protected Scene Scene;

		protected DisplayMode Mode;
		protected IIntersector[] Intersectors;
		// The next settings for this raycaster to prevent race conditions from setting outside the render thread
		private DisplayMode NextMode = DisplayMode.Primitives;
		protected IIntersector[] NextIntersectors = null;

		private int MaxBoundingBoxes = -1;

		public DebugRaycaster(Scene scene)
		{
			Scene = scene;
		}

		public void SetMode(DisplayMode mode)
		{
			if (mode == DisplayMode.Selection && NextIntersectors == null)
			{
				if (NextMode == DisplayMode.Selection)
					NextMode = DisplayMode.Primitives;
				return;
			}

			NextMode = mode;
		}

		public void ClearDisplayOnly()
		{
			NextIntersectors = null;
		}

		/// <summary>
		/// Set the only item to display in the overlay to the provided object.
		/// </summary>
		/// <param name="item">The item to display. Valid types are <see cref="Primitive"/>, <see cref="IObject"/> and <see cref="BVH{Primitive}"/> of <see cref="Primitive"/>.</param>
		/// <returns>Whether the item provided was valid to display on the overlay.</returns>
		public bool SetDisplayOnly(object item)
		{
			ClearDisplayOnly();
			bool valid = false;

			if (item is Primitive primitive)
			{
				NextIntersectors = new PrimitiveIntersector[] { new PrimitiveIntersector(primitive) };
				valid = true;
			}
			else if (item is IObject parent)
			{
				NextIntersectors = Scene.Primitives.Where(p => p.Parent == parent).Select(p => new PrimitiveIntersector(p)).ToArray();
				valid = true;
			}
			else if (item is BVH<Primitive> node)
			{
				NextIntersectors = new BVHIntersector[] { new BVHIntersector(node) };
				valid = true;
			}

			SetMode(DisplayMode.Selection);
			return valid;
		}

		protected Color GetColorFromID(int id)
		{
			Util.Assert(id >= 0, "Debug color ID cannot be negative.");

			return ColorRotation[id % ColorRotation.Length];
		}

		public Color GetColor(Ray ray)
		{
			switch (Mode)
			{
				case DisplayMode.Selection:
					double dist = double.PositiveInfinity;
					IIntersector intersector = null;

					foreach (IIntersector cur in Intersectors)
					{
						double curD = cur.Intersect(ray);

						if (curD <= dist)
						{
							dist = curD;
							intersector = cur;
						}
					}

					if (intersector == null)
						return Color.Transparent;

					return GetColorFromID(intersector.ID);
				case DisplayMode.Primitives:
					Hit hit = Scene.RayTrace(ray, null);

					if (hit == default)
						return Color.Transparent;

					return GetColorFromID(Math.Max(hit.Primitive.ID, 0));
				case DisplayMode.BoundingVolumes:
					if (!Scene.HasAccelerator)
						return Color.Transparent;

					int count = Scene.Accelerator.GetIntersectionCount(ray);

					if (MaxBoundingBoxes < count)
						MaxBoundingBoxes = count;

					if (count == 0)
						return Color.Transparent;

					return Color.FromArgb(Math.Min(count, 255), 255, 255, 255);
				default:
					throw new Exception($"Unknown debug display mode {Mode}.");
			}
		}

		public Bitmap RenderDebug()
		{
			Stopwatch timer = new Stopwatch();
			timer.Start();

			int w = Scene.Width;
			int h = Scene.Height;
			Camera camera = Scene.Camera;
			Bitmap output = new Bitmap(w, h);

			// Change to the next settings set from outside threads here to prevent race conditions
			Mode = NextMode;
			Intersectors = NextIntersectors;

			unsafe
			{
				BitmapData data = output.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				int* values = (int*)data.Scan0.ToPointer();

				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						Ray ray = camera.GetRay(x, y).Offset(camera.imagePlane);
						Color color = GetColor(ray);

						switch (Mode)
						{
							case DisplayMode.BoundingVolumes:
								double a = Math.Sqrt(color.A / (double)MaxBoundingBoxes);
								color = Color.FromArgb((int)(a * 255), color);
								break;
							default:
								color = Color.FromArgb(color.A / 2, color);
								break;
						}

						values[(y * data.Width) + x] = color.ToArgb();
					}
				}

				output.UnlockBits(data);
			}

			Trace.WriteLine($"Debug render took {timer.Elapsed.TotalMilliseconds}ms.");

			return output;
		}
	}
}
