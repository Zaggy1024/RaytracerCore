using System;
using System.Collections.Generic;
using System.Text;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Acceleration
{
	public interface IBoundingVolume : IBoundedObject
	{
		/// <summary>
		/// Intersect a ray with the bounding shape.
		/// </summary>
		/// <param name="ray">The ray to intersect.</param>
		/// <returns>A tuple of the near and far hit distances, or NaN if missed.</returns>
		public (double near, double far) Intersect(Ray ray);

		public List<(string name, object value)> GetProperties();

		public double GetVolume();
	}
}
