using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Acceleration
{
	/// <summary>
	/// Axis-Aligned Bounding Box
	/// </summary>
	class AABB : IBoundingVolume
	{
		const double Expand = 1e-12;

		public static AABB CreateSized(Vec4D center, Vec4D size)
		{
			size /= 2;
			return new AABB(center - size, center + size);
		}

		public static AABB CreateFromBounded(IBoundedObject bounded)
		{
			Vec4D center = bounded.GetCenter();
			return new AABB(
				center - new Vec4D(
					bounded.GetMaxCenterDistance(new Vec4D(-1, 0, 0, 0)) + Expand,
					bounded.GetMaxCenterDistance(new Vec4D(0, -1, 0, 0)) + Expand,
					bounded.GetMaxCenterDistance(new Vec4D(0, 0, -1, 0)) + Expand,
					0),
				center + new Vec4D(
					bounded.GetMaxCenterDistance(new Vec4D(1, 0, 0, 0)) + Expand,
					bounded.GetMaxCenterDistance(new Vec4D(0, 1, 0, 0)) + Expand,
					bounded.GetMaxCenterDistance(new Vec4D(0, 0, 1, 0)) + Expand,
					0));
		}

		public static AABB Combine(IBoundedObject a, IBoundedObject b)
		{
			Vec4D center = a.GetCenter();
			Vec4D aMin = center - new Vec4D(
					a.GetMaxCenterDistance(new Vec4D(-1, 0, 0, 0)),
					a.GetMaxCenterDistance(new Vec4D(0, -1, 0, 0)),
					a.GetMaxCenterDistance(new Vec4D(0, 0, -1, 0)),
					0);
			Vec4D aMax = center + new Vec4D(
					a.GetMaxCenterDistance(new Vec4D(1, 0, 0, 0)),
					a.GetMaxCenterDistance(new Vec4D(0, 1, 0, 0)),
					a.GetMaxCenterDistance(new Vec4D(0, 0, 1, 0)),
					0);

			center = b.GetCenter();
			Vec4D bMin = center - new Vec4D(
					b.GetMaxCenterDistance(new Vec4D(-1, 0, 0, 0)),
					b.GetMaxCenterDistance(new Vec4D(0, -1, 0, 0)),
					b.GetMaxCenterDistance(new Vec4D(0, 0, -1, 0)),
					0);
			Vec4D bMax = center + new Vec4D(
					b.GetMaxCenterDistance(new Vec4D(1, 0, 0, 0)),
					b.GetMaxCenterDistance(new Vec4D(0, 1, 0, 0)),
					b.GetMaxCenterDistance(new Vec4D(0, 0, 1, 0)),
					0);

			return new AABB(Vec4D.Min(aMin, bMin), Vec4D.Max(aMax, bMax));
		}

		private readonly Vec4D Minimum;
		private readonly Vec4D Maximum;

		public AABB(Vec4D minimum, Vec4D maximum)
		{
			Minimum = minimum;
			Maximum = maximum;
		}

		public Vec4D GetCenter()
		{
			return (Minimum + Maximum) / 2;
		}

		public double GetMaxCenterDistance(Vec4D direction)
		{
			Vec4D center = GetCenter();
			double dist = 0;

			if (direction == Vec4D.Zero)
			{
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Minimum.Z, 1) - center).Length, dist);
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Maximum.Z, 1) - center).Length, dist);

				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Minimum.Z, 1) - center).Length, dist);
				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Maximum.Z, 1) - center).Length, dist);

				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Minimum.Z, 1) - center).Length, dist);
				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Maximum.Z, 1) - center).Length, dist);

				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Minimum.Z, 1) - center).Length, dist);
				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Maximum.Z, 1) - center).Length, dist);
			}
			else
			{
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Minimum.Z, 1) - center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Maximum.Z, 1) - center).Dot(direction), dist);

				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Minimum.Z, 1) - center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Maximum.Z, 1) - center).Dot(direction), dist);

				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Minimum.Z, 1) - center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Maximum.Z, 1) - center).Dot(direction), dist);

				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Minimum.Z, 1) - center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Maximum.Z, 1) - center).Dot(direction), dist);
			}

			return dist;
		}

		public (double near, double far) IntersectAVX(Ray ray)
		{
			Vector256<double> origin = (Vector256<double>)ray.Origin;
			Vector256<double> direction = (Vector256<double>)ray.Direction;

			Vector256<double> min = (Vector256<double>)Minimum;
			Vector256<double> max = (Vector256<double>)Maximum;

			Vector256<double> dirMask = Avx.Compare(direction, new Vector256<double>(), FloatComparisonMode.OrderedLessThanNonSignaling);

			Vector256<double> minMasked = Avx.BlendVariable(min, max, dirMask);
			Vector256<double> maxMasked = Avx.BlendVariable(max, min, dirMask);

			direction = Avx.Divide(Vector256.Create(1D), direction);
			Vector256<double> near4 = Avx.Multiply(Avx.Subtract(minMasked, origin), direction);
			Vector256<double> far4 = Avx.Multiply(Avx.Subtract(maxMasked, origin), direction);

			double near;
			double far;

			near = near4.ToScalar();
			double temp = SIMDHelpers.Swap(near4.GetLower()).ToScalar();
			near = temp > near ? temp : near;
			temp = near4.GetUpper().ToScalar();
			near = temp > near ? temp : near;

			far = far4.ToScalar();
			temp = SIMDHelpers.Swap(far4.GetLower()).ToScalar();
			far = temp < far ? temp : far;
			temp = far4.GetUpper().ToScalar();
			far = temp < far ? temp : far;

			if (near > far | !(far >= 0))
				return (double.NaN, double.NaN);

			return (near, far);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vec4D SelectFromSign(Vec4D positive, Vec4D negative, Vec4D mask)
		{
			return new Vec4D(
				mask.X < 0 ? negative.X : positive.X,
				mask.Y < 0 ? negative.Y : positive.Y,
				mask.Z < 0 ? negative.Z : positive.Z,
				positive.W);
		}

		public (double near, double far) Intersect(Ray ray)
		{
			if (SIMDHelpers.Enabled)
				return IntersectAVX(ray);

			Vec4D d = ray.Direction;

			Vec4D min = Minimum;
			Vec4D max = Maximum;
			Vec4D temp = min;

			min = SelectFromSign(min, max, d);
			max = SelectFromSign(max, temp, d);

			min = (min - ray.Origin) / d;
			max = (max - ray.Origin) / d;

			double near = min.X;
			double far = max.X;
			// Can't use Math max/min functions as they will propagate NaN
			near = min.Y > near ? min.Y : near;
			far = max.Y < far ? max.Y : far;
			near = min.Z > near ? min.Z : near;
			far = max.Z < far ? max.Z : far;

			if (near > far | !(far >= 0))
				return (double.NaN, double.NaN);

			return (near, far);
		}

		public double GetVolume()
		{
			Vec4D size = Maximum - Minimum;
			return size.X * size.Y * size.Z;
		}

		public List<(string name, object value)> GetProperties()
		{
			return new List<(string name, object value)>
			{
				("Minimum", Minimum),
				("Maximum", Maximum)
			};
		}
	}
}
