using System;
using System.Collections.Generic;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	/// <summary>
	/// Infinite plane primitive. VERY UNTESTED!
	/// </summary>
	public class Plane : Primitive
	{
		protected Vec4D Normal;
		protected double OriginDistance;

		public Plane() { }

		public Plane(double origin, Vec4D normal)
		{
			OriginDistance = origin;
			Normal = normal.Normalize();
		}

		public override Vec4D GetCenter()
		{
			return new Vec4D(0, 0, 0, 1) + Normal * OriginDistance;
		}

		public override void Transform(Mat4x4D forward, Mat4x4D inverse)
		{
			Vec4D center = forward * GetCenter();
			Normal = (inverse.Transpose3x3() * Normal).Normalize();
			OriginDistance = center.Dot(Normal);
		}

		internal override Hit[] DoRayTrace(Ray ray)
		{
			double rayDist = ray.Origin.Dot(Normal);
			double denom = ray.Direction.Dot(Normal);
		
			if (Util.NearlyEqual(denom, 0) && Util.NearlyEqual(OriginDistance, rayDist))	 // Ray is along plane
				return new Hit[] { new Hit(this, ray.Origin, 0, Normal, true) };
			
			if (denom == 0)
				return default;
			
			double dist = (OriginDistance - rayDist) / denom;
			
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

		public override double GetMaxCenterDistance(Vec4D direction)
		{
			if (Math.Abs(Normal.Dot(direction)) == 1)
				return 0;

			return double.PositiveInfinity;
		}

		public override List<(string name, object value)> Properties
		{
			get
			{
				var properties = base.Properties;

				properties.Add(("Origin", OriginDistance));
				properties.Add(("Normal", Normal));

				return properties;
			}
		}

		public override string Name => "Plane";

		public override string ToString()
		{
			return $"{Name} @ [{OriginDistance}] N [{Normal}]";
		}
	}
}
