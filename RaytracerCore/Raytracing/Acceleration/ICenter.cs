using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Acceleration
{
	public interface ICenter
	{
		/// <summary>
		/// Get the center of this object.
		/// </summary>
		public Vec4D GetCenter();
	}
}
