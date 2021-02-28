using System;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Acceleration
{
	public interface IBoundedObject
	{
		/// <summary>
		/// Get the center of the bounds of this object.
		/// </summary>
		public Vec4D GetCenter();

		/// <summary>
		/// Gets the maximum distance from the center to any point on the object, using the provided function to calculate the distance to each point.
		/// 
		/// <paramref name="direction"/> may be (0, 0, 0, 0) to get the maximum distance from the center to any point on the object.
		/// </summary>
		/// <param name="direction">The axis along which to calculate the maximum distance.</param>
		public double GetMaxCenterDistance(Vec4D direction);
	}
}
