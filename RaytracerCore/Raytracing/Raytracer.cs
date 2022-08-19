using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Drawing;

using RaytracerCore.Vectors;
using RaytracerCore.Raytracing.Cameras;

namespace RaytracerCore.Raytracing
{
	public class Raytracer
	{
		public enum BounceType
		{
			Skipped,
			Diffuse,
			Specular,
			SpecularFail,
			Transmitted,
			Emission,
			PureBlack,
			RecursionComplete,
			Missed,
			Debug
		}

		public class DebugRay
		{
			public Hit Hit = default;
			public BounceType Type = BounceType.Skipped;
			public double FresnelRatio = double.NaN;
		}

		public FullRaytracer FullRaytracer;
		public Scene Scene;
		
		public bool Stop = false;
		public volatile bool Running = false;

		private readonly Random Rand;
		
		public Raytracer(FullRaytracer fullRaytracer, Scene scene)
		{
			FullRaytracer = fullRaytracer;
			Scene = scene;

			Rand = new Random();
		}

		private Vec4D RandomShine(Vec4D dir, double shininess)
		{
			double z = shininess == double.PositiveInfinity ? 1 : Math.Pow(Rand.NextDouble(), 1 / shininess);
			double theta = Rand.NextDouble() * Math.PI * 2;
			return Vec4D.CreateHorizon(dir, z, theta);
		}

		private Vec4D Reflection(Vec4D normal, Vec4D incoming, double cos)
		{
			return incoming + (normal * (cos * 2));
		}

		// Inline to optimize out the debug code when not using the inspector.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private DoubleColor GetColor(Ray ray, ref DebugRay[] debug)
		{
			Hit prevHit = default;
			Hit hit = default;
			DoubleColor tint = new DoubleColor(1);

			for (int i = 0; i <= Scene.Recursion; i++)
			{
				// Periodically normalize the direction vector to prevent compounding error
				if (i % 3 == 0)
					ray = Ray.Directional(ray.Origin, ray.Direction);

				hit = Scene.RayTrace(ray, prevHit);

				DebugRay debugRay = debug != null ? debug[i] = new DebugRay() { Hit = hit } : null;

				if (hit == default)
				{
					if (debugRay != null) debugRay.Type = BounceType.Missed;

					// Return emission or -1 for instant misses
					if (i == 0)
						return DoubleColor.Placeholder;

					// Return miss color for misses
					return Scene.AmbientRGB;
				}

				if (Scene.DebugGeom)
				{
					if (debugRay != null) debugRay.Type = BounceType.Debug;

					return hit.Primitive.Specular + hit.Primitive.Diffuse + hit.Primitive.Emission;
				}

				if (i >= Scene.Recursion)
				{
					if (debugRay != null) debugRay.Type = BounceType.RecursionComplete;
					break;
				}

				Ray outRay = Ray.Zero;

				Vec4D roughNormal = RandomShine(hit.Normal, hit.Primitive.Shininess);

				double diffLum = hit.Primitive.Diffuse.Luminance;
				double specLum = hit.Primitive.Specular.Luminance;
				double refrLum = hit.Primitive.Refraction.Luminance;
				double emisLum = hit.Primitive.Emission.Luminance;

				double cos = -roughNormal.Dot(ray.Direction);
				double cosOut = 0;
				double iorRatio = 0;

				// Calculate ratio of reflection to transmission on this hit
				if ((refrLum > 0 | specLum > 0) && hit.Primitive.RefractiveIndex != 0 && cos >= 0)
				{
					double iorIn;
					double iorOut;

					if (hit.Inside)
					{
						iorIn = hit.Primitive.RefractiveIndex;
						iorOut = Scene.AirRefractiveIndex;
					}
					else
					{
						iorIn = Scene.AirRefractiveIndex;
						iorOut = hit.Primitive.RefractiveIndex;
					}

					iorRatio = iorIn / iorOut;
					double sinOut = iorRatio * Math.Sqrt(1 - (cos * cos));

					// Skip refraction when we get total internal reflection
					if (sinOut >= 1)
					{
						refrLum = 0;

						if (debugRay != null) debugRay.FresnelRatio = 1;
					}
					else
					{
						cosOut = Math.Sqrt(1 - (sinOut * sinOut));
						double ratioSWave = ((iorOut * cos) - (iorIn * cosOut)) / ((iorOut * cos) + (iorIn * cosOut));
						double ratioPWave = ((iorIn * cos) - (iorOut * cosOut)) / ((iorIn * cos) + (iorOut * cosOut));
						double ratio = ((ratioSWave * ratioSWave) + (ratioPWave * ratioPWave)) / 2;
						specLum *= ratio;
						refrLum *= 1 - ratio;

						if (debugRay != null) debugRay.FresnelRatio = ratio;
					}
				}
				else
				{
					refrLum = 0;
				}

				double totalLum = diffLum + specLum + refrLum + emisLum;

				if (totalLum <= 0)
				{
					if (debugRay != null) debugRay.Type = BounceType.PureBlack;
					break;
				}

				if (i >= Scene.Recursion)
				{
					if (debugRay != null) debugRay.Type = BounceType.RecursionComplete;
					break;
				}

				DoubleColor newTint = DoubleColor.Black;
				double rayRand = Rand.NextDouble() * totalLum;

				// Choose whether to do a specular bounce based on brightness of specular
				if (refrLum != 0 && (rayRand -= refrLum) <= 0)
				{
					// Transmission implementation
					if (debugRay != null) debugRay.Type = BounceType.Transmitted;

					Vec4D outDir = (roughNormal * -cosOut) + ((ray.Direction + (roughNormal * cos)) * iorRatio);
					outRay = new Ray(hit.Position, outDir);
					newTint = hit.Primitive.Refraction;

					// Only tint on entering the object, and don't count the recursivity
					if (hit.Inside)
						newTint = new DoubleColor(1);
				}
				else if (specLum != 0 && (rayRand -= specLum) <= 0)
				{
					if (debugRay != null) debugRay.Type = BounceType.SpecularFail;

					// Specular reflection
					Vec4D outDir = Reflection(roughNormal, ray.Direction, cos);

					// Determine whether the rough normal reflection is heading outward before continuing
					if (outDir.Dot(hit.Normal) > 0)
					{
						if (debugRay != null) debugRay.Type = BounceType.Specular;

						outRay = new Ray(hit.Position, outDir);
						newTint = hit.Primitive.Specular;
					}
				}
				else if (diffLum != 0 && (rayRand -= diffLum) <= 0)
				{
					// Diffuse reflection
					if (debugRay != null) debugRay.Type = BounceType.Diffuse;

					double z = (2 * Math.Acos(Rand.NextDouble())) / Math.PI;
					double theta = Rand.NextDouble() * Math.PI * 2;
					outRay = new Ray(hit.Position, Vec4D.CreateHorizon(hit.Normal, z, theta));
					newTint = hit.Primitive.Diffuse;
				}
				else
				{
					// Emission
					if (debugRay != null) debugRay.Type = BounceType.Emission;

					Util.Assert(hit.Primitive.Emission != DoubleColor.Black, "Emission being returned with no luminance");

					// Break out of the loop to return emission early
					break;
				}

				if (outRay == Ray.Zero)
					break;

				prevHit = hit;
				ray = outRay;
				// Since we limit the total brightness of the reflection through our bounce selection above,
				// normalize to the total luminosity of our combined luminosities
				newTint = newTint * Math.Max(totalLum, 1);

				tint = tint * newTint;

				//color = color.add(hit.Primitive.Emission.mult(tint));
			}

			return tint * hit.Primitive.Emission;
		}

		public DoubleColor GetColor(Ray ray)
		{
			DebugRay[] debug = null;
			return GetColor(ray, ref debug);
		}

		public DebugRay[] GetDebugTrace(Ray ray)
		{
			// Add 1 to the trace length so that we have room for the last hit returning emission color.
			DebugRay[] debug = new DebugRay[Scene.Recursion + 1];
			GetColor(ray, ref debug);
			return debug.TakeWhile((r) => r != null).ToArray();
		}

		public static Ray GetCameraRay(Camera camera, Random rand, int x, int y)
		{
			double dofAmt = camera.dofAmount;
			double subX = x + rand.NextDouble();
			double subY = y + rand.NextDouble();
			Ray ray = camera.GetRay(subX, subY).Offset(camera.imagePlane);

			if (dofAmt != 0)
			{
				Vec4D focusPoint = ray.GetPoint(camera.focalLength - camera.imagePlane);
				double dist = Math.Sqrt(rand.NextDouble()) * dofAmt;
				double angle = rand.NextDouble() * Math.PI * 2;

				double offX = Math.Cos(angle) * dist;
				double offY = Math.Sin(angle) * dist;

				ray = camera.GetRay(subX + offX, subY + offY).Offset(camera.imagePlane).PointingTowards(focusPoint);
			}

			return ray;
		}

		public DoubleColor GetColor(int x, int y)
		{
			return GetColor(GetCameraRay(Scene.Camera, Rand, x, y));
		}

		public DebugRay[] GetDebugTrace(int x, int y)
		{
			return GetDebugTrace(GetCameraRay(Scene.Camera, Rand, x, y));
		}

		public void Render()
		{
			while (Running) { }

			Running = true;

			Stopwatch stopwatch = new Stopwatch();

			while (true)
			{
				Rectangle tile = FullRaytracer.GetWorkingTile();
				DoubleColor[,] samples = new DoubleColor[tile.Width, tile.Height];

				stopwatch.Restart();

				for (int y = 0; y < tile.Height && !Stop; y++)
				{
					for (int x = 0; x < tile.Width; x++)
					{
						PixelPos pos = new PixelPos(tile.Left + x, tile.Top + y);

						for (int i = 0; i < 1; i++)
						{
							samples[x, y] = GetColor(pos.X, pos.Y);
						}
					}
				}

				// Break before the tile is submitted so that partial tile updates aren't sent
				if (Stop)
					break;

				FullRaytracer.OnTileFinished(tile, samples, stopwatch.Elapsed);
			}

			Running = false;
		}
	}
}