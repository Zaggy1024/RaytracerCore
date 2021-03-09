using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

using RaytracerCore.Raytracing;
using RaytracerCore.Vectors;

namespace RaytracerCore
{
	/// <summary>
	/// Misc. utility functions used elsewhere in the code.
	/// </summary>
	public static class Util
	{
		public static readonly double NearEnough = 1e-24;

		/// <summary>Format a <see cref="TimeSpan"/> for human readability.</summary>
		/// <param name="time">The time span to format.</param>
		public static string FormatTimeSpan(TimeSpan time)
		{
			string result = "";

			if (time.Days > 0)
				result += $"{time.Days} days ";
			if (result != "" || time.Hours > 0)
				result += $"{time.Hours:D}:";
			if (result != "" || time.Minutes > 0)
				result += $"{time.Minutes:D2}:";

			return $"{result}{time.Seconds:D2}.{time.Milliseconds:D3}";
		}

		/// <summary>Determine whether a delta value is within the error margin of two operand values.</summary>
		/// <param name="a">The first value used to determine the error margin.</param>
		/// <param name="b">The second value used to determine the error margin.</param>
		/// <param name="delta">The difference between the values to compare.</param>
		/// <returns>Whether the delta is less than the expected error margin.</returns>
		public static bool NearlyEqual(double a, double b, double delta)
		{
			const double minNormal = double.Epsilon * 1e7;

			if (delta == 0)
				return true;

			delta = Math.Abs(delta);

			return delta <= minNormal || delta / Math.Max(a, b) < NearEnough;
		}

		/// <summary>Compare two doubles for near equality, using the absolute values to determine error margin.</summary>
		/// <param name="a">The first value to compare.</param>
		/// <param name="b">The second value to compare.</param>
		/// <returns>Whether the difference is less than the expected error margin.</returns>
		public static bool NearlyEqual(double a, double b)
		{
			return NearlyEqual(a, b, a - b);
		}

		[Conditional("TRACE")]
		public static void Assert(bool value, string error)
		{
			if (!value)
				throw new Exception(error);
		}

		[Conditional("TRACE")]
		public static void AssertNearlyEqual(double a, double b, double allowed, string error)
		{
			if (Math.Abs(a - b) > allowed)
				throw new Exception($"{error}: {a} ~= {b} ({Math.Abs(a - b)})");
		}

		[Conditional("TRACE")]
		public static void AssertNearlyEqual(double a, double b, string error)
		{
			if (!NearlyEqual(a, b))
				throw new Exception($"{error}: {a} ~= {b} ({Math.Abs(a - b)})");
		}

		[Conditional("TRACE")]
		public static void AssertNearlyEqual(Vec4D a, Vec4D b, double allowed, string error)
		{
			if (Math.Abs((a - b).SquaredLength) > allowed * allowed)
				throw new Exception($"{error}: {a} ~= {b} ({Math.Abs((a - b).Length)} > {allowed})");
		}

		/// <summary>Determine whether a value is within a minimum and maximum value range (both inclusive).</summary>
		/// <param name="value">The value to compare.</param>
		/// <param name="min">The minimum value.</param>
		/// <param name="max">The maximum value.</param>
		/// <returns>Whether the value is within the specified range.</returns>
		public static bool ValueInRange(double value, double min, double max)
		{
			return value >= min && value <= max;
		}

		/// <summary>
		/// Limits a value within the specified range.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="minimum">The minimum value to return.</param>
		/// <param name="maximum">The maximum value to return.</param>
		/// <returns>The clamped value.</returns>
		public static double Clamp(double value, double minimum, double maximum)
		{
			if (value < minimum)
				return minimum;
			if (value > maximum)
				return maximum;
			return value;
		}

		/// <summary>
		/// Limits a value within the specified range.
		/// </summary>
		/// <param name="value">The value to clamp.</param>
		/// <param name="minimum">The minimum value to return.</param>
		/// <param name="maximum">The maximum value to return.</param>
		/// <returns>The clamped value.</returns>
		public static int Clamp(int value, int minimum, int maximum)
		{
			if (value < minimum)
				return minimum;
			if (value > maximum)
				return maximum;
			return value;
		}

		/// <summary>Determine whether two hits should be considered similar enough to be ignored by the ray tracer.</summary>
		/// <param name="ray">The ray that resulted in the <paramref name="b"/> hit.</param>
		/// <param name="a">The first hit to compare.</param>
		/// <param name="b">The second hit to compare.</param>
		/// <returns>Whether the hits are considered the same.</returns>
		public static bool RayHitMatches(Ray ray, Hit a, Hit b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Primitive != b.Primitive)
				return false;
			if (!a.Position.NearlyEquals(b.Position))
				return false;
			if (ray.Direction.Dot(b.Normal) > 0)
				return a.Inside != b.Inside;
			return a.Inside == b.Inside;
		}
	}
}
