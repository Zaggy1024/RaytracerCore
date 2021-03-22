using RaytracerCore.Raytracing.Primitives;
using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Objects
{
	public class ObjectConsts
	{
		public static readonly string ImplicitInstance = "implicit";
	}

	public interface IObject
	{
		Primitive[] GetChildren(string instance);

		//void Transform(Matrix transform, Matrix inverseTransform);

		string Name { get; }
	}
}
