//#define HIT_STRUCT

using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing
{
#if HIT_STRUCT
	public readonly struct Hit
#else
	public class Hit
#endif
	{
		public readonly Primitive Primitive;

		public readonly Vec4D Position;
		public readonly double Distance;

		public readonly Vec4D Normal;
		public readonly bool Inside;

#if TRACE
		public string DebugText;
#endif

		public Hit(Primitive primitive, Vec4D position, double distance, Vec4D normal, bool inside)
		{
			Util.Assert(primitive != null, "Cannot initialize Hit primitive to null.");

			Primitive = primitive;

			Position = position;
			Distance = distance;

			Normal = normal;
			Inside = inside;
		}

		public Hit Inverted()
		{
			return new Hit(Primitive, Position, Distance, Normal, !Inside);
		}

		public static bool operator ==(in Hit left, in Hit right)
		{
#if !HIT_STRUCT
			if (left is null)
				return right is null;
			if (right is null)
				return left is null;
#endif

			return
				left.Primitive == right.Primitive &&
				left.Position == right.Position &&
				left.Distance == right.Distance &&
				left.Normal == right.Normal &&
				left.Inside == right.Inside;
		}

		public static bool operator !=(in Hit left, in Hit right)
		{
			return !(left == right);
		}
	}
}
