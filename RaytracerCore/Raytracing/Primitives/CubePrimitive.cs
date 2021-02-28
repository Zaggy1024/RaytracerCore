using System;
using System.Runtime.CompilerServices;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	/// <summary>
	/// Cube as a primitive, currently only supporting axis-aligned shapes.
	/// </summary>
	class CubePrimitive : Primitive
	{
		private readonly Vec4D Minimum;
		private readonly Vec4D Maximum;

		public CubePrimitive(Vec4D center, Vec4D size)
		{
			size /= 2;
			Minimum = center - size;
			Maximum = center + size;
		}

		public override void Transform(Mat4x4D objectMatrix, Mat4x4D worldMatrix)
		{
		}

		internal override Hit[] DoRayTrace(Ray ray)
		{
			Vec4D d = ray.Direction;

			Vec4D min = Minimum;
			Vec4D max = Maximum;
			Vec4D temp = min;

			min = new Vec4D(d.X < 0 ? max.X : min.X, d.Y < 0 ? max.Y : min.Y, d.Z < 0 ? max.Z : min.Z, min.W);
			max = new Vec4D(d.X < 0 ? temp.X : max.X, d.Y < 0 ? temp.Y : max.Y, d.Z < 0 ? temp.Z : max.Z, max.W);

			min = (min - ray.Origin) / d;
			max = (max - ray.Origin) / d;

			double near = min.X;
			int nearSide = 0;
			double far = max.X;
			int farSide = 0;

			if (min.Y > near)
			{
				near = min.Y;
				nearSide = 1;
			}

			if (max.Y < far)
			{
				far = max.Y;
				farSide = 1;
			}

			if (min.Z > near)
			{
				near = min.Z;
				nearSide = 2;
			}

			if (max.Z < far)
			{
				far = max.Z;
				farSide = 2;
			}

			if (near > far | !(far >= 0))
				return default;

			Vec4D farPt = ray.GetPoint(far);
			Vec4D farN = farSide switch
			{
				0 => new Vec4D(Math.CopySign(1, -d.X), 0, 0, 0),
				1 => new Vec4D(0, Math.CopySign(1, -d.Y), 0, 0),
				2 => new Vec4D(0, 0, Math.CopySign(1, -d.Z), 0),
				_ => throw new Exception("impossible")
			};

			if (!(near >= 0))
			{
				return new Hit[] { new Hit(this, farPt, far, farN, true) };
			}

			Vec4D nearPt = ray.GetPoint(near);
			Vec4D nearN = nearSide switch
			{
				0 => new Vec4D(Math.CopySign(1, -d.X), 0, 0, 0),
				1 => new Vec4D(0, Math.CopySign(1, -d.Y), 0, 0),
				2 => new Vec4D(0, 0, Math.CopySign(1, -d.Z), 0),
				_ => throw new Exception("impossible")
			};

			return new Hit[] {
				new Hit(this, nearPt, near, nearN, false),
				new Hit(this, farPt, far, farN, true)
			};
		}

		public override Vec4D GetCenter()
		{
			return (Minimum + Maximum) / 2;
		}

		public override double GetMaxCenterDistance(Vec4D direction)
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
	}
}
