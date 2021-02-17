using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

using RaytracerCore.Vectors;
using RaytracerCore.Raytracing.Cameras;
using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Raytracing.Objects;

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

		public List<Primitive> Primitives = new List<Primitive>();

		public int Recursion = 3;

		public double AirRefractiveIndex = 1.000293;

		public static Hit RayTracePrimitives(Ray ray, Hit skipHit, IEnumerable<Primitive> primitives)
		{
			double curDist = double.PositiveInfinity;
			Hit outHit = default;

			foreach (Primitive prim in primitives)
			{
				Hit hit = prim.RayTrace(ray, skipHit);

#if DEBUG
				prim.RayTrace(ray, skipHit);
#endif

				if (hit == default(Hit) || hit.Distance >= curDist)
					continue;

				curDist = hit.Distance;
				outHit = hit;

			}

			return outHit;
		}

		public Hit RayTrace(Ray ray, Hit skipHit)
		{
			return RayTracePrimitives(ray, skipHit, Primitives);
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
	}
}
