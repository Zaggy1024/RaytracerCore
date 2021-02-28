using System;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	/// <summary>
	/// Infinite plane primitive. VERY UNTESTED!
	/// </summary>
	public class Plane : Primitive
	{
		protected Ray Line;
		protected Vec4D Normal;
		protected double OriginDist;

		public Plane() { }

		public Plane(Ray line, Vec4D normal)
		{
			Line = line;
			Vec4D side = line.Direction.Cross(normal).Normalize();
			Normal = side.Cross(line.Direction).Normalize();
			OriginDist = Normal.Dot(line.Origin);
		}

		public override void Transform(Mat4x4D forward, Mat4x4D inverse)
		{
			Normal = (forward * Normal).Normalize();
			Line = Line.Transform(forward);

			// TODO: Make this reuse normal calculation from constructor, this isn't guaranteed to be a right angle
			// Normal calculation from constructor likely results in a flipped normal, investigate
		}

		internal override Hit[] DoRayTrace(Ray ray)
		{
			double rayDist = ray.Origin.Dot(Normal);
			double denom = ray.Direction.Dot(Normal);
		
			if (Util.NearlyEqual(denom, 0) && Util.NearlyEqual(OriginDist, rayDist))	 // Ray is along plane
				return new Hit[] { new Hit(this, ray.Origin, 0, Normal, true) };
			
			if (denom == 0)
				return default;
			
			double dist = (OriginDist - rayDist) / denom;
			
			if (dist >= -Util.NearEnough)
			{
				Vec4D hitPos = ray.GetPoint(dist);
				Vec4D hitNormal = Normal;
				bool inside = false;

				// If ray is in the same direction as normal, we're on the inside
				if (Normal.Dot(ray.Direction) > 0)
				{
					hitNormal = -hitNormal;
					inside = true;
				}

				return new Hit[] { new Hit(this, hitPos, (hitPos - ray.Origin).Length, hitNormal, inside) };
			}
			
			return default;
		}

		public override Vec4D GetCenter()
		{
			return Line.Origin;
		}

		public override double GetMaxCenterDistance(Vec4D direction)
		{
			if (Math.Abs(Normal.Dot(direction)) == 1)
				return 0;

			return double.PositiveInfinity;
		}
	}
}
