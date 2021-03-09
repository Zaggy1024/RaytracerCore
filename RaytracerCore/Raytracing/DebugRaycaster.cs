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
			Scene Scene;
			Primitive Primitive;

			public PrimitiveIntersector(Scene scene, Primitive primitive)
			{
				Scene = scene;
				Primitive = primitive;
			}

			public double Intersect(Ray ray)
			{
				Hit hit = Primitive.RayTrace(ray, null);

				if (hit != default)
					return hit.Distance;

				return double.NaN;
			}

			public int ID => Scene.GetPrimitiveID(Primitive);
		}

		public class BVHIntersector : IIntersector
		{
			Scene Scene;
			BVH<Primitive> Node;

			public BVHIntersector(Scene scene, BVH<Primitive> node)
			{
				Scene = scene;
				Node = node;
			}

			public double Intersect(Ray ray)
			{
				var intersect = Node.Volume.Intersect(ray);

				if (intersect.near >= 0)
					return intersect.near;

				return intersect.far;
			}

			public int ID
			{
				get
				{
					int id = Scene.GetBVHLeafID(Node);
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
				NextIntersectors = new PrimitiveIntersector[] { new PrimitiveIntersector(Scene, primitive) };
				valid = true;
			}
			else if (item is IObject parent)
			{
				NextIntersectors = Scene.Primitives.Where(p => p.Parent == parent).Select(p => new PrimitiveIntersector(Scene, p)).ToArray();
				valid = true;
			}
			else if (item is BVH<Primitive> node)
			{
				NextIntersectors = new BVHIntersector[] { new BVHIntersector(Scene, node) };
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

						if (curD < dist)
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

					return GetColorFromID(Scene.GetPrimitiveID(hit.Primitive));
				case DisplayMode.BoundingVolumes:
					BoundingIntersection<Primitive> collision = Scene.Accelerator.IntersectAll(ray).FirstOrDefault();

					if (collision == null)
						return Color.Transparent;

					int id = Scene.GetBVHLeafID(collision.Node);

					// Handle branches as well using their hashcodes, since they won't be included in the scene list
					if (id < 0)
						id = collision.Node.GetHashCode();

					return GetColorFromID(id);
				default:
					throw new Exception($"Unknown debug display mode {Mode}.");
			}
		}

		public Bitmap RenderDebug()
		{
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

				for (int x = 0; x < w; x++)
				{
					for (int y = 0; y < h; y++)
					{
						Ray ray = camera.GetRay(x, y).Offset(camera.imagePlane);
						values[(y * data.Width) + x] = GetColor(ray).ToArgb();

						/*if (x == 375 && y == 520)
							GetColor(ray);*/
					}
				}

				output.UnlockBits(data);
			}

			return output;
		}
	}
}
