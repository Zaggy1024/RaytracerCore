using System;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;

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

		protected PixelPos[] Pixels;
		
		public ulong Samples = 0;
		
		public volatile bool Stop = false;
		public volatile bool Running = false;
		public volatile bool Paused = false;

		private Random Rand;
		
		public Raytracer(FullRaytracer fullRaytracer, Scene scene, PixelPos[] pixels)
		{
			FullRaytracer = fullRaytracer;
			Scene = scene;
			Pixels = pixels;

			Rand = new Random();
		}

		private Vec4D RandomShine(Vec4D dir, double shininess)
		{
			double z = shininess == double.PositiveInfinity ? 1 : Math.Pow(Rand.NextDouble(), 1 / shininess);
			double theta = Rand.NextDouble() * Math.PI * 2;
			return Vec4D.CreateHorizon(dir, z, theta);
		}

		private Vec4D Reflection(Vec4D normal, Vec4D incoming)
		{
			return incoming - (normal * (incoming.Dot(normal) * 2));
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

#if DEBUG
				Scene.RayTrace(ray, prevHit);
#endif

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

				// We don't need to handle 
				if (hit.Primitive.Shininess == 0)
				{
					specLum = 0;
					refrLum = 0;
				}

				double iorRatio = 0;

				// Calculate ratio of reflection to transmission on this hit
				if (hit.Primitive.RefractiveIndex != 0)
				{
					double cosIn = -roughNormal.Dot(ray.Direction);

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
					double sinOut = iorRatio * Math.Sqrt(1 - (cosIn * cosIn));

					// Skip refraction when we get total internal reflection
					if (sinOut >= 1)
					{
						refrLum = 0;

						if (debugRay != null) debugRay.FresnelRatio = 1;
					}
					else
					{
						cosIn = Math.Abs(cosIn);
						double cosOut = Math.Sqrt(1 - (sinOut * sinOut));
						double ratioSWave = ((iorOut * cosIn) - (iorIn * cosOut)) / ((iorOut * cosIn) + (iorIn * cosOut));
						double ratioPWave = ((iorIn * cosIn) - (iorOut * cosOut)) / ((iorIn * cosIn) + (iorOut * cosOut));
						double ratio = ((ratioSWave * ratioSWave) + (ratioPWave * ratioPWave)) / 2;
						specLum *= ratio;
						refrLum *= 1 - ratio;

						if (debugRay != null) debugRay.FresnelRatio = ratio;
					}
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
				Vec4D outDir;
				double rayRand = Rand.NextDouble() * totalLum;
				double radicand;

				// Choose whether to do a specular bounce based on brightness of specular
				if ((rayRand -= refrLum) <= 0)
				{
					// Transmission implementation
					double ratio = iorRatio;
					double cos = -roughNormal.Dot(ray.Direction);
					radicand = 1 - (ratio * ratio * (1 - (cos * cos)));

					Util.Assert(radicand > 0, "Fresnel equations did not prevent total internal reflection in refraction code");

					if (debugRay != null) debugRay.Type = BounceType.Transmitted;

					outDir = (ratio * ray.Direction) + (((ratio * cos) - Math.Sqrt(radicand)) * roughNormal);
					outRay = Ray.Directional(hit.Position, outDir);
					newTint = hit.Primitive.Refraction;

					// Only tint on entering the object, and don't count the recursivity
					if (hit.Inside)
						newTint = new DoubleColor(1);
				}
				else if ((rayRand -= specLum) <= 0)
				{
					if (debugRay != null) debugRay.Type = BounceType.SpecularFail;

					// Specular reflection
					roughNormal = RandomShine(hit.Normal, hit.Primitive.Shininess);
					outDir = Reflection(roughNormal, ray.Direction);

					if (outDir.Dot(hit.Normal) > 0)
					{
						if (debugRay != null) debugRay.Type = BounceType.Specular;

						outRay = Ray.Directional(hit.Position, outDir);
						newTint = hit.Primitive.Specular;
					}
				}
				else if ((rayRand -= diffLum) <= 0)
				{
					// Diffuse reflection
					if (debugRay != null) debugRay.Type = BounceType.Diffuse;

					double z = (2 * Math.Acos(Rand.NextDouble())) / Math.PI;
					double theta = Rand.NextDouble() * Math.PI * 2;
					outRay = Ray.Directional(hit.Position, Vec4D.CreateHorizon(hit.Normal, z, theta));
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

		public Ray GetCameraRay(Camera camera, int x, int y)
		{
			double dofAmt = camera.dofAmount;
			double subX = x + Rand.NextDouble();
			double subY = y + Rand.NextDouble();
			Ray ray = camera.GetRay(subX, subY).Offset(camera.imagePlane);

			if (dofAmt != 0)
			{
				Vec4D focusPoint = ray.GetPoint(camera.focalLength - camera.imagePlane);
				double dist = Math.Sqrt(Rand.NextDouble()) * dofAmt;
				double angle = Rand.NextDouble() * Math.PI * 2;

				double offX = Math.Cos(angle) * dist;
				double offY = Math.Sin(angle) * dist;

				ray = camera.GetRay(subX + offX, subY + offY).Offset(camera.imagePlane).PointingTowards(focusPoint);
			}

			return ray;
		}

		public DoubleColor GetColor(int x, int y)
		{
			return GetColor(GetCameraRay(Scene.Camera, x, y));
		}

		public DebugRay[] GetDebugTrace(int x, int y)
		{
			return GetDebugTrace(GetCameraRay(Scene.Camera, x, y));
		}

		public void Render()
		{
			while (Running) { }

			Running = true;

			while (!Stop)
			{
				for (uint pixel = 0; pixel < Pixels.Length && !Stop; pixel++)
				{
					PixelPos pos = Pixels[pixel];

					for (int i = 0; i < 1; i++)
					{
						DoubleColor color = GetColor(pos.x, pos.y);

						if (color == DoubleColor.Placeholder)
							FullRaytracer.AddMiss(pos);
						else
							FullRaytracer.AddSample(pos, color);

						Samples++;
					}

					if (!Stop && FullRaytracer.IsPaused)
					{
						Paused = true;
						FullRaytracer.WaitForResume();
						Paused = false;
					}
				}
			}

			Stop = false;
			Running = false;
		}
	}
}