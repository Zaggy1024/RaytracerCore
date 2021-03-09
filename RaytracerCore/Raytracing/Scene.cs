#define BVH

using System.Drawing;
using System.Collections.Generic;
using System.Linq;

using RaytracerCore.Vectors;
using RaytracerCore.Raytracing.Cameras;
using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Raytracing.Acceleration;

namespace RaytracerCore.Raytracing
{
	public class Scene
	{
		public int Width;
		public int Height;

		public DoubleColor BackgroundRGB = DoubleColor.Black;
		public double BackgroundAlpha = 0;

		public DoubleColor AmbientRGB = DoubleColor.Black;

		public bool DebugGeom;

		public int CurrentCamera = 0;
		public List<Camera> Cameras = new List<Camera>();

		private List<Primitive> _Primitives = new List<Primitive>();
		private BVH<Primitive> _Accelerator = null;
		private List<BVH<Primitive>> AcceleratorFlat = null;

		public int Recursion = 3;

		public double AirRefractiveIndex = 1.000293;

		public void Prepare()
		{
#if BVH
			if (_Accelerator == null)
			{
				_Accelerator = BVH.Construct(_Primitives);
				AcceleratorFlat = _Accelerator.Flatten().ToList();
			}
#endif
		}

		public void ResetAccelerator()
		{
			_Accelerator = null;
			AcceleratorFlat = null;
		}

		public void AddPrimitive(Primitive primitive)
		{
			_Primitives.Add(primitive);
			ResetAccelerator();
		}

		public int GetPrimitiveID(Primitive primitive)
		{
			return _Primitives.IndexOf(primitive);
		}

		public int GetBVHLeafID(BVH<Primitive> node)
		{
			return AcceleratorFlat.IndexOf(node);
		}

		public static Hit RayTracePrimitives(Ray ray, Hit skipHit, IEnumerable<Primitive> primitives, BVH<Primitive> Accelerator)
		{
			Hit hit = default;

			// If we have an accelerator, filter down to only the needed primitives.
#if BVH
			if (Accelerator != null)
			{
				List<BoundingIntersection<Primitive>> intersections = Accelerator.IntersectAll(ray);
				BoundingIntersection<Primitive> previous = null;

				foreach (BoundingIntersection<Primitive> current in intersections)
				{
					if (previous != null && current.Near > previous.Far)
						break;

					Hit currentHit = current.Object.RayTrace(ray, skipHit);

					if (currentHit != default &&
						(hit == default || currentHit.Distance < hit.Distance))
					{
						hit = currentHit;
						previous = current;
					}
				}
			}
			else
#endif
			{
				foreach (Primitive prim in primitives)
				{
					Hit currentHit = prim.RayTrace(ray, skipHit);

#if DEBUG
					prim.RayTrace(ray, skipHit);
#endif

					if (currentHit != default &&
						(hit == default || currentHit.Distance < hit.Distance))
						hit = currentHit;
				}
			}

			return hit;
		}

		public Hit RayTrace(Ray ray, Hit skipHit)
		{
			Util.Assert(_Accelerator != null, "Accelerator must be initialized before rendering.");

			return RayTracePrimitives(ray, skipHit, _Primitives, _Accelerator);
		}

		public Camera Camera
		{
			get => Cameras[CurrentCamera];
		}

		public bool NextCamera()
		{
			CurrentCamera++;

			if (CurrentCamera >= Cameras.Count)
			{
				CurrentCamera = 0;
				return true;
			}

			return false;
		}

		public Color BackgroundColor
		{
			get
			{
				Color color = BackgroundRGB.ToColor();
				return Color.FromArgb(color.R, color.G, color.B, (int)(BackgroundAlpha * 255) & 255);
			}

			set
			{
				BackgroundRGB = new DoubleColor(value.R / 255.0, value.G / 255.0, value.B / 255.0);
				BackgroundAlpha = value.A / 255.0;
			}
		}

		public BVH<Primitive> Accelerator => _Accelerator;

		public IList<Primitive> Primitives => _Primitives.AsReadOnly();
	}
}
