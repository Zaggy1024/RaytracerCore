using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RaytracerCore.Vectors;

namespace RaytracerCore
{
	public class MatrixStack : Stack<Mat4x4D>
	{
		public MatrixStack() : base()
		{
			Push(Mat4x4D.Identity4x4);
		}

		public void Push()
		{
			Push(Peek());
		}

		public void Transform(Mat4x4D matrix)
		{
			Push(Pop() * matrix);
		}

		public void InvTransform(Mat4x4D matrix)
		{
			Push(matrix * Pop());
		}
	}
}
