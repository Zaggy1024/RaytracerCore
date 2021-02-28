using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	public class Triangle : Primitive
	{
		public static Triangle CreateRectangle(Ray up, Vec4D normal, double width, double height)
		{
			Vec4D side = up.Direction.Cross(normal).Normalize();
			Vec4D v0 = up.Origin + (up.Direction * (-height / 2)) + (side * (-width / 2));
			Vec4D v1 = v0 + (side * width);
			Vec4D v2 = v0 + (up.Direction * height);
			return new Triangle(v0, v1, v2, true);
		}

		protected Vertex Vert0;
		protected Vertex Vert1;
		protected Vertex Vert2;
		protected bool Mirror = false;
		protected bool HasNormals = false;
		protected Vec4D Edge0to1;
		protected Vec4D Edge0to2;
		protected Vec4D Normal;

		public Triangle(Vec4D p0, Vec4D p1, Vec4D p2, bool mirror)
		{
			Vert0 = new Vertex(p0);
			Vert1 = new Vertex(p1);
			Vert2 = new Vertex(p2);
			Recalculate();

			Mirror = mirror;
		}

		public Triangle(Vec4D p0, Vec4D p1, Vec4D p2) : this(p0, p1, p2, false)
		{
		}

		public Triangle(Vertex v0, Vertex v1, Vertex v2)
		{
			Vert0 = v0;
			Vert1 = v1;
			Vert2 = v2;
			HasNormals = true;
			Recalculate();
		}

		private void Recalculate()
		{
			Edge0to1 = Vert1.Position - Vert0.Position;
			Edge0to2 = Vert2.Position - Vert0.Position;

			if (!HasNormals)
			{
				Normal = Edge0to1.Cross(Edge0to2).Normalize();
				Vert0 = Vert0.WithNormal(Normal);
				Vert1 = Vert1.WithNormal(Normal);
				Vert2 = Vert2.WithNormal(Normal);
			}
		}

		public override void Transform(Mat4x4D forward, Mat4x4D inverse)
		{
			Vert0 = Vert0.Transformed(forward);
			Vert1 = Vert1.Transformed(forward);
			Vert2 = Vert2.Transformed(forward);
			Recalculate();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		private Hit[] RayTraceAVXFaster(Ray ray)
		{
			Vector256<double> dir = (Vector256<double>)ray.Direction;
			Vector256<double> vert0 = (Vector256<double>)Vert0.Position;
			Vector256<double> edge0to1 = (Vector256<double>)Edge0to1;
			Vector256<double> edge0to2 = (Vector256<double>)Edge0to2;

			Vector256<double> offset = Avx.Subtract((Vector256<double>)ray.Origin, vert0);
			Vector256<double> side1 = SIMDHelpers.Cross(offset, edge0to1);
			Vector256<double> side2 = SIMDHelpers.Cross(dir, edge0to2);

			// Prepare all dot products
			Vector256<double> uvTemp = Avx.Multiply(offset, side2);	// u
			Vector256<double> temp = Avx.Multiply(dir, side1);		// v
			Vector256<double> edge2Temp = Avx.Multiply(edge0to2, side1);
			Vector256<double> distTemp = Avx.Multiply(edge0to1, side2);
			uvTemp = Avx.HorizontalAdd(uvTemp, temp);
			edge2Temp = Avx.HorizontalAdd(edge2Temp, edge2Temp);
			distTemp = Avx.HorizontalAdd(distTemp, distTemp);

			// Complete all dot products for SSE ops
			Vector128<double> uvs = SIMDHelpers.Add2(uvTemp);
			Vector128<double> dist = SIMDHelpers.Add2(edge2Temp);
			Vector128<double> temp1 = SIMDHelpers.Add2(distTemp);
			Vector128<double> temp2;

			// vec2 constants we'll be using later
			Vector128<double> ones2 = SIMDHelpers.BroadcastScalar2(1D);
			Vector128<double> zeroes2 = new Vector128<double>();

			// Reciprocal of distance along edge0to1
			temp1 = Sse2.Divide(ones2, temp1);
			temp2 = Sse2.CompareOrdered(temp1, temp1);
			// Remove NaNs from the result, replaced with 0
			Vector128<double> distZeroed = Sse2.And(temp1, temp2);

			uvs = Sse2.Multiply(uvs, distZeroed);
			dist = Sse2.Multiply(dist, distZeroed);

			// compare uvs < 0 and > 1, dist < 0, jump out if any of those conditions are met
			temp1 = Sse2.CompareLessThan(uvs, zeroes2);
			temp2 = Mirror ? uvs : Sse3.HorizontalAdd(uvs, uvs);
			temp2 = Sse2.CompareGreaterThan(temp2, ones2);
			temp1 = Sse2.Or(temp1, temp2);
			temp2 = Sse2.CompareLessThan(dist, zeroes2);
			temp1 = Sse2.Or(temp1, temp2);

			if (!Avx.TestZ(temp1, temp1))
				return default;

			bool inside = Sse2.CompareScalarOrderedLessThan(distZeroed, zeroes2);
			double u = uvs.ToScalar();
			double v = SIMDHelpers.Swap(uvs).ToScalar();

			Vec4D result = (Vec4D)Fma.MultiplyAdd(edge0to1, Vector256.Create(u), Fma.MultiplyAdd(edge0to2, Vector256.Create(v), vert0));

#if DEBUG
			/*Util.Assert(dist < -Util.NearEnough, "triangle hit behind");
			Util.AssertNearlyEqual(result.Sub(ray.start).Length, dist, 1e-8, "triangle hit distance inaccurate");
			Util.AssertNearlyEqual(result.Sub(ray.start.Add(ray.dir.Mult(dist))).SquareDistance, 0, 1e-8, "triangle hit pos too far");*/
#endif

			return new Hit[] {
				new Hit(primitive: this,
					position: result,
					distance: dist.ToScalar(),
					normal: GetNormal(u, v, inside),
					inside: inside)
			};
		}

		internal override Hit[] DoRayTrace(Ray ray)
		{
			if (SIMDHelpers.Enabled)
				return RayTraceAVXFaster(ray);

			Vec4D side = ray.Direction.Cross(Edge0to2);
			// Reciprocal of distance until later
			double dist = Edge0to1.Dot(side);
			double u;
			double v;
			Vec4D offset = ray.Origin - Vert0.Position;
			bool inside = true;

			if (dist == 0)
			{   // Ray start is along our triangle plane, check UVs
				u = Edge0to1.Dot(offset);
				v = Edge0to2.Dot(offset);

				if (u < 0 | u > 1 |
					v < 0 | (Mirror ? v : u + v) > 1)
					return default;

				dist = 0;
			}
			else
			{   // Ray may be passing through our triangle, check projected UVs
				// Get actual distance from ray start
				dist = 1 / dist;
				u = dist * offset.Dot(side);
				offset = offset.Cross(Edge0to1);
				v = dist * ray.Direction.Dot(offset);
				inside = dist < 0;
				dist *= Edge0to2.Dot(offset);

				// Check:
				// if we're outside u of the triangle
				// if we're outside v of the triangle
				// if we're intersecting behind our ray
				// Evaluate all to avoid too many jump instructions
				if (u < 0 | u > 1 |
					v < 0 | (Mirror ? v : u + v) > 1 |
					dist < -Util.NearEnough)
					return default;
			}

			offset = Vert0.Position + (Edge0to1 * u) + (Edge0to2 * v);

#if DEBUG
			Util.Assert(dist > -Util.NearEnough, "triangle hit behind");
			Util.AssertNearlyEqual((offset - ray.Origin).Length, dist, 1e-8, "triangle hit distance inaccurate");
			Util.AssertNearlyEqual((offset - ray.GetPoint(dist)).SquaredLength, 0, 1e-8, "triangle hit pos too far");
#endif

			return new Hit[] { new Hit(primitive: this,
				position: offset,
				distance: dist,
				normal: GetNormal(u, v, inside),
				inside: inside)
			};
		}

		protected virtual Vec4D GetNormal(double u, double v, bool inside)
		{
			if (HasNormals)
			{
				Vec4D normal = ((Vert0.Normal * u) +
					(Vert1.Normal * v) +
					(Vert2.Normal * (u + v))).Normalize();
				if (inside)
					return normal - (Normal * (2 * (normal.Dot(Normal)) / Normal.Dot(Normal)));
				return normal;
			}

			if (inside)
				return Normal * -1;
			return Normal;
		}

		public override Vec4D GetCenter()
		{
			return (Vert0.Position + Vert1.Position + Vert2.Position) / 3;
		}

		public override double GetMaxCenterDistance(Vec4D direction)
		{
			Vec4D center = GetCenter();
			double dist = 0;

			if (direction == Vec4D.Zero)
			{
				dist = Math.Max((Vert0.Position - center).Length, dist);
				dist = Math.Max((Vert1.Position - center).Length, dist);
				dist = Math.Max((Vert2.Position - center).Length, dist);
			}
			else
			{
				dist = Math.Max((Vert0.Position - center).Dot(direction), dist);
				dist = Math.Max((Vert1.Position - center).Dot(direction), dist);
				dist = Math.Max((Vert2.Position - center).Dot(direction), dist);
			}

			return dist;
		}
	}
}
