namespace RaytracerCore.Raytracing.Acceleration
{
	public class BoundingIntersection<T> where T : class, IBoundedObject
	{
		public BVH<T> Node;
		public double Near;
		public double Far;

		public BoundingIntersection(BVH<T> node, double near, double far)
		{
			Node = node;
			Near = near;
			Far = far;
		}

		public T Object => Node.Object;
	}
}
