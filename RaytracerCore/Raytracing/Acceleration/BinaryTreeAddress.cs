using System.Text;
using System.Collections.Generic;

using static RaytracerCore.Raytracing.Acceleration.BinaryTreeAddress;

namespace RaytracerCore.Raytracing.Acceleration
{
	public static class BinaryTreeAddress
	{
		public enum Side
		{
			Left,
			Right
		}
	}

	public class BinaryTreeAddress<T>
	{
		private readonly Side[] Address;
		public readonly bool Found;
		public readonly T Object;

		public BinaryTreeAddress(Side[] address, bool found, T obj)
		{
			Address = address;
			Found = found;
			Object = obj;
		}

		public BinaryTreeAddress(List<Side> address, bool found, T obj) : this(address.ToArray(), found, obj)
		{
		}

		public Side this[int index] => Address[index];

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			foreach (Side side in Address)
			{
				switch (side)
				{
					case Side.Left:
						builder.Append("L");
						break;
					case Side.Right:
						builder.Append("R");
						break;
				}
			}

			builder.Append(Found ? " (Found)" : " (Not found)");

			return builder.ToString();
		}
	}
}
