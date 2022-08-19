using System;

namespace RaytracerCore.Vectors
{
	public class MatrixTransforms
	{
		public static Mat4x4D Translate(double x, double y, double z)
		{
			return new Mat4x4D(
				1, 0, 0, x,
				0, 1, 0, y,
				0, 0, 1, z,
				0, 0, 0, 1);
		}

		public static Mat4x4D Scale(double x, double y, double z)
		{
			return new Mat4x4D(
				x, 0, 0, 0,
				0, y, 0, 0,
				0, 0, z, 0,
				0, 0, 0, 1);
		}

		public static Mat4x4D Rotate(double angle, Vec4D axis)
		{
			double cos = Math.Cos(angle);
			double sin = Math.Sin(angle);

			double cosOpp = 1 - cos;

			return new Mat4x4D(
				cos + axis.X * axis.X * cosOpp,				axis.X * axis.Y * cosOpp - axis.Z * sin,	axis.X * axis.Z * cosOpp + axis.Y * sin,	0,
				axis.Y * axis.X * cosOpp + axis.Z * sin,	cos + axis.Y * axis.Y * cosOpp,				axis.Y * axis.Z * cosOpp - axis.X * sin,	0,
				axis.Z * axis.X * cosOpp - axis.Y * sin,	axis.Z * axis.Y * cosOpp + axis.X * sin,	cos + axis.Z * axis.Z * cosOpp,				0,
				0,											0,											0,											1);
		}
	}
}
