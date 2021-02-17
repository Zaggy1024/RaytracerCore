using System;
using System.Text.RegularExpressions;

using RaytracerCore.Vectors;
using RaytracerCore.Raytracing.Primitives;

namespace RaytracerCore.Raytracing.Objects
{
	public class Cube : IObject
	{
		[Flags]
		public enum Side
		{
			XPos = 1,
			XNeg = 2,
			YPos = 4,
			YNeg = 8,
			ZPos = 16,
			ZNeg = 32
		}

		public static Side GetSide(string name)
		{
			if (name == ObjectConsts.ImplicitInstance)
				return 0;
			if (name == "all")
				return AllSides;

			if (name[0] == '-' && name.Length == 2)
			{
				switch (name[1])
				{
					case 'x':
						return Side.XNeg;
					case 'y':
						return Side.YNeg;
					case 'z':
						return Side.ZNeg;
				}
			}

			char axis = ' ';

			if (name[0] == '+' && name.Length == 2)
				axis = name[1];
			else if (name.Length == 1)
				axis = name[0];

			if (axis != ' ')
			{
				switch (axis)
				{
					case 'x':
						return Side.XPos;
					case 'y':
						return Side.YPos;
					case 'z':
						return Side.ZPos;
				}
			}

			throw new ArgumentException($"Unknown Cube side name {name}.", name);
		}

		public static readonly Side NoSides = 0;
		public static readonly Side AllSides = Side.XPos | Side.XNeg | Side.YPos | Side.YNeg | Side.ZPos | Side.ZNeg;

		protected Vec4D Position;
		protected Vec4D Size;

		private Triangle CreateRect(Vec4D pos, Vec4D up, Vec4D norm, double dist, double width, double height)
		{
			Triangle prim = Triangle.CreateRectangle(Ray.Directional(pos + (norm * (dist / 2)), up), norm, width, height);
			prim.Parent = this;
			return prim;
		}

		public Cube(Vec4D position, Vec4D size)
		{
			Position = position;
			Size = size;
		}

		/*public void Transform(Matrix transform, Matrix inverseTransform)
		{
			foreach (Primitive prim in prims)
				prim.Transform(transform, inverseTransform);
		}*/

		public Primitive[] GetChildren(Side sides)
		{
			Primitive[] prims = new Primitive[6];
			int i = 0;

			if ((sides & Side.XPos) != 0)
				prims[i++] = CreateRect(Position, new Vec4D(0, 0, 1, 0), new Vec4D(1, 0, 0, 0), Size.X, Size.Y, Size.Z);
			if ((sides & Side.XNeg) != 0)
				prims[i++] = CreateRect(Position, new Vec4D(0, 0, -1, 0), new Vec4D(-1, 0, 0, 0), Size.X, Size.Y, Size.Z);

			if ((sides & Side.YPos) != 0)
				prims[i++] = CreateRect(Position, new Vec4D(0, 0, 1, 0), new Vec4D(0, 1, 0, 0), Size.Y, Size.X, Size.Z);
			if ((sides & Side.YNeg) != 0)
				prims[i++] = CreateRect(Position, new Vec4D(0, 0, -1, 0), new Vec4D(0, -1, 0, 0), Size.Y, Size.X, Size.Z);

			if ((sides & Side.ZPos) != 0)
				prims[i++] = CreateRect(Position, new Vec4D(0, 1, 0, 0), new Vec4D(0, 0, 1, 0), Size.Z, Size.X, Size.Y);
			if ((sides & Side.ZNeg) != 0)
				prims[i++] = CreateRect(Position, new Vec4D(0, -1, 0, 0), new Vec4D(0, 0, -1, 0), Size.Z, Size.X, Size.Y);

			Array.Resize(ref prims, i);
			return prims;
		}

		public Primitive[] GetChildren(string instance)
		{
			return GetChildren(GetSide(instance));
		}
	}
}
