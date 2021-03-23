using System;

namespace RaytracerCore.Vectors
{
	/// <summary>
	/// <para>Represents a ray to cast through a scene.</para>
	/// <para>This implementation includes no maximum distance for a ray cast, but one may be desired
	/// to prevent floating point inaccuracies affecting the output.</para>
	/// </summary>
	public struct Ray
	{
		public static readonly Ray Zero = new Ray();

		/// <summary>Returns a ray starting from <paramref name="origin"/> and extending infinitely toward the <paramref name="end"/> point.</summary>
		public static Ray FromTo(Vec4D origin, Vec4D end)
		{
			return new Ray(origin, (end - origin).Normalize());
		}

		/// <summary>Create a directional ray starting from <paramref name="origin"/>. The <paramref name="direction"/> vector will be normalized.</summary>
		public static Ray Directional(Vec4D origin, Vec4D direction)
		{
			return new Ray(origin, direction.Normalize());
		}

		/// <summary>The origin point.</summary>
		public readonly Vec4D Origin;
		/// <summary>The vector direction.</summary>
		public readonly Vec4D Direction;

		/// <summary>Used to initialize a <see cref="Ray"/> with an already-normalized <paramref name="direction"/> vector.</summary>
		public Ray(Vec4D origin, Vec4D direction)
		{
			Origin = origin;
			Direction = direction;

#if DEBUG
			Util.AssertNearlyEqual(direction.SquaredLength, 1, 1e-9, "Ray was instantiated with a non-unit direction vector.");
#endif
		}

		/// <summary>Creates a new <see cref="Ray"/> transformed by the <paramref name="matrix"/> parameter, with the new direction vector normalized.</summary>
		public Ray Transform(Mat4x4D matrix)
		{
#if DEBUG
			Util.AssertNearlyEqual(matrix * Direction, matrix * (Origin + Direction) - (matrix * Origin), 0.00001, "Ray direction doesn't transform correctly");
#endif

			return Directional(matrix * Origin, matrix * Direction);
		}

		/// <summary>Creates a new <see cref="Vec4D"/> at the specified <paramref name="distance"/> along the ray.</summary>
		public Vec4D GetPoint(double distance)
		{
			return Origin + (Direction * distance);
		}

		/// <summary>Creates a new <see cref="Ray"/>, with the origin offset by <paramref name="distance"/>.</summary>
		public Ray Offset(double distance)
		{
			return new Ray(GetPoint(distance), Direction);
		}

		/// <summary>A point <see cref="Vec4D"/>, offset from the origin by the direction vector.</summary>
		public Vec4D End
		{
			get => Origin + Direction;
		}

		/// <summary>Create a new <see cref="Ray"/> pointing towards the specified <paramref name="position"/>.</summary>
		public Ray PointingTowards(Vec4D position)
		{
			return FromTo(Origin, position);
		}

		public double GetDistanceTo(Vec4D point)
		{
			Vec4D toPoint = point - Origin;
			/*// More accurate?
			double distance = toPoint.Length;
			Vec4D toPointN = toPoint / distance;
			double cos = Direction.Dot(toPointN);
			double sin = Math.Sqrt(1 - cos * cos);
			return distance * sin;*/

			return (toPoint - (Direction * toPoint.Dot(Direction))).Length;
		}

		public static bool operator ==(Ray left, Ray right)
		{
			return left.Origin == right.Origin && left.Direction == right.Direction;
		}

		public static bool operator !=(Ray left, Ray right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj is Ray ray)
				return this == ray;

			return false;
		}

		public override int GetHashCode()
		{
			return Origin.GetHashCode() ^ Direction.GetHashCode();
		}
	}
}
