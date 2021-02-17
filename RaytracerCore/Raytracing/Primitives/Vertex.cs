using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	public class Vertex
	{
		public readonly Vec4D Position;
		public readonly Vec4D Normal;

		public Vertex(Vec4D pos, Vec4D normal)
		{
			Position = pos;
			Normal = normal.Normalize();
		}

		public Vertex(Vec4D pos) : this(pos, new Vec4D(0, 0, 1, 0))
		{
		}

		public Vertex WithNormal(Vec4D normal)
		{
			return new Vertex(Position, normal);
		}

		public Vertex Transformed(Mat4x4D matrix)
		{
			return new Vertex(matrix * Position, (matrix * Normal).Normalize());
		}
	}
}
