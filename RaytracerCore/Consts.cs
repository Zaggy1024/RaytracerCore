using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaytracerCore
{
	public class Consts
	{
		private const double RAD2DEG = Math.PI / 180;

		public static double toRadians(double degrees)
		{
			return degrees * RAD2DEG;
		}
		public static double toDegrees(double radians)
		{
			return radians / RAD2DEG;
		}
	}
}
