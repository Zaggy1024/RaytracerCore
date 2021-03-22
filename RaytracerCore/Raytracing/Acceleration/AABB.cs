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
					bounded.GetMaxCenterDistance(new Vec4D(-1, 0, 0, 0)),
					bounded.GetMaxCenterDistance(new Vec4D(0, -1, 0, 0)),
					bounded.GetMaxCenterDistance(new Vec4D(0, 0, -1, 0)),
					0),
				center + new Vec4D(
					bounded.GetMaxCenterDistance(new Vec4D(1, 0, 0, 0)),
					bounded.GetMaxCenterDistance(new Vec4D(0, 1, 0, 0)),
					bounded.GetMaxCenterDistance(new Vec4D(0, 0, 1, 0)),
					0));
		}

		public static AABB Combine(IBoundedObject a, IBoundedObject b)
		{
			AABB aBB = a is AABB aAsBox ? aAsBox : CreateFromBounded(a);
			AABB bBB = b is AABB bAsBox ? bAsBox : CreateFromBounded(b);
			return new AABB(Vec4D.Min(aBB.Minimum, bBB.Minimum), Vec4D.Max(aBB.Maximum, bBB.Maximum));
		}

		private readonly Vec4D Minimum;
		private readonly Vec4D Maximum;
		private readonly Vec4D Size;
		private readonly Vec4D Center;

		public AABB(Vec4D minimum, Vec4D maximum)
		{
			Minimum = minimum;
			Maximum = maximum;
			Size = Maximum - Minimum;
			// Replace NaN with 0 for cases where infinity 
			Center = (Minimum + (Size / 2)).WithDefault(0);

#if TRACE
			Util.Assert(!Minimum.HasNaN(), $"Attempted to create a bounding box with an invalid minimum: ({Minimum})");
			Util.Assert(!Maximum.HasNaN(), $"Attempted to create a bounding box with an invalid maximum: ({Maximum})");
			Util.Assert(!Size.HasNaN(), $"Creating a bounding box resulted in an invalid size: ({Size})");
			Util.Assert(!Center.HasNaN(), $"Creating a bounding box resulted in an invalid center: ({Center})");
#endif
		}

		public Vec4D GetCenter()
		{
			return Center;
		}

		public double GetMaxCenterDistance(Vec4D direction)
		{
			double dist = 0;

			if (direction == Vec4D.Zero)
			{
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Minimum.Z, 1) - Center).Length, dist);
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Maximum.Z, 1) - Center).Length, dist);

				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Minimum.Z, 1) - Center).Length, dist);
				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Maximum.Z, 1) - Center).Length, dist);

				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Minimum.Z, 1) - Center).Length, dist);
				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Maximum.Z, 1) - Center).Length, dist);

				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Minimum.Z, 1) - Center).Length, dist);
				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Maximum.Z, 1) - Center).Length, dist);
			}
			else
			{
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Minimum.Z, 1) - Center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Minimum.X, Minimum.Y, Maximum.Z, 1) - Center).Dot(direction), dist);

				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Minimum.Z, 1) - Center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Minimum.X, Maximum.Y, Maximum.Z, 1) - Center).Dot(direction), dist);

				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Minimum.Z, 1) - Center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Maximum.X, Minimum.Y, Maximum.Z, 1) - Center).Dot(direction), dist);

				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Minimum.Z, 1) - Center).Dot(direction), dist);
				dist = Math.Max((new Vec4D(Maximum.X, Maximum.Y, Maximum.Z, 1) - Center).Dot(direction), dist);
			}

			return dist;
		}

		public (double near, double far) IntersectAVX(Ray ray)
		{
			Vector256<double> origin = (Vector256<double>)ray.Origin;
			Vector256<double> direction = (Vector256<double>)ray.Direction;

			Vector256<double> zeroes = new Vector256<double>();
			Vector256<double> min = (Vector256<double>)Minimum;
			Vector256<double> max = (Vector256<double>)Maximum;

			// Replace slabs that won't be checked (0 direction axis) with infinity so that NaN doesn't propagate
			Vector256<double> dirInfMask = Avx.And(
				Avx.Compare(direction, zeroes, FloatComparisonMode.OrderedEqualNonSignaling),
				Avx.And(
					Avx.Compare(origin, min, FloatComparisonMode.OrderedGreaterThanOrEqualNonSignaling),
					Avx.Compare(origin, min, FloatComparisonMode.OrderedLessThanOrEqualNonSignaling)));
			min = Avx.BlendVariable(min, SIMDHelpers.BroadcastScalar4(double.NegativeInfinity), dirInfMask);
			max = Avx.BlendVariable(max, SIMDHelpers.BroadcastScalar4(double.PositiveInfinity), dirInfMask);

			// Flip slabs in direction axes that are negative
			Vector256<double> dirNegMask = Avx.Compare(direction, zeroes, FloatComparisonMode.OrderedLessThanNonSignaling);
			Vector256<double> minMasked = Avx.BlendVariable(min, max, dirNegMask);
			Vector256<double> maxMasked = Avx.BlendVariable(max, min, dirNegMask);

			direction = Avx.Divide(Vector256.Create(1D), direction);
			Vector256<double> near4 = Avx.Multiply(Avx.Subtract(minMasked, origin), direction);
			Vector256<double> far4 = Avx.Multiply(Avx.Subtract(maxMasked, origin), direction);

			Vector128<double> near2 = Sse2.Max(near4.GetLower(), near4.GetUpper());
			near2 = Sse2.MaxScalar(near2, SIMDHelpers.Swap(near2));
			Vector128<double> far2 = Sse2.Min(far4.GetLower(), far4.GetUpper());
			far2 = Sse2.MinScalar(far2, SIMDHelpers.Swap(far2));

			if (Sse2.CompareScalarOrderedGreaterThan(near2, far2) | Sse2.CompareScalarOrderedLessThan(far2, new Vector128<double>()))
				return (double.NaN, double.NaN);

			return (near2.ToScalar(), far2.ToScalar());
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

			// Fix cases of zeroed direction axes
			if (ray.Direction.Z == 0 || ray.Direction.Y == 0 || ray.Direction.Z == 0)
			{
				min = new Vec4D(
					(double.IsNaN(min.X) && Util.ValueInRange(ray.Origin.X, Minimum.X, Maximum.X)) ? double.NegativeInfinity : min.X,
					(double.IsNaN(min.Y) && Util.ValueInRange(ray.Origin.Y, Minimum.Y, Maximum.Y)) ? double.NegativeInfinity : min.Y,
					(double.IsNaN(min.Z) && Util.ValueInRange(ray.Origin.Z, Minimum.Z, Maximum.Z)) ? double.NegativeInfinity : min.Z,
					min.W);
				max = new Vec4D(
					(double.IsNaN(max.X) && Util.ValueInRange(ray.Origin.X, Minimum.X, Maximum.X)) ? double.PositiveInfinity : max.X,
					(double.IsNaN(max.Y) && Util.ValueInRange(ray.Origin.Y, Minimum.Y, Maximum.Y)) ? double.PositiveInfinity : max.Y,
					(double.IsNaN(max.Z) && Util.ValueInRange(ray.Origin.Z, Minimum.Z, Maximum.Z)) ? double.PositiveInfinity : max.Z,
					min.W);
			}

			double near = min.X;
			double far = max.X;
			near = Math.Max(min.Y, near);
			near = Math.Max(min.Z, near);
			far = Math.Min(max.Y, far);
			far = Math.Min(max.Z, far);

			if (near >= far | !(far >= 0))
				return (double.NaN, double.NaN);

			return (near, far);
		}

		public double GetVolume()
		{
			return Size.X * Size.Y * Size.Z;
		}

		public double GetSurfaceArea()
		{
			return ((Size.X * Size.Y) + (Size.Y * Size.Z) + (Size.Z * Size.X)) * 2;
		}

		public List<(string name, object value)> Properties
		{
			get
			{
				return new List<(string name, object value)>
				{
					("Minimum", Minimum),
					("Maximum", Maximum)
				};
			}
		}

		public bool NearlyEquals(IBoundingVolume volume)
		{
			if (this == volume)
				return true;

			if (volume is AABB aabb)
				return Util.NearlyEqual(Minimum, aabb.Minimum) && Util.NearlyEqual(Maximum, aabb.Maximum);

			return false;
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
				return true;

			if (obj is AABB other)
				return Minimum == other.Minimum && Maximum == other.Maximum;

			return false;
		}

		public override int GetHashCode()
		{
			return Minimum.GetHashCode() ^ Maximum.GetHashCode();
		}
	}
}
