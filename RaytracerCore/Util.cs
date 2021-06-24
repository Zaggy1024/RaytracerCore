using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using RaytracerCore.Raytracing;
using RaytracerCore.Vectors;
using System.Runtime.CompilerServices;

namespace RaytracerCore
{
	/// <summary>
	/// Misc. utility functions used elsewhere in the code.
	/// </summary>
	public static class Util
	{
		public const double NearEnough = 1e-24;

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

		/// <summary>
		/// Compare two doubles for near equality, using the absolute values to determine error margin.
		/// </summary>
		/// <param name="a">The first value to compare.</param>
		/// <param name="b">The second value to compare.</param>
		/// <returns>Whether the difference is less than the expected error margin.</returns>
		public static bool NearlyEqual(double a, double b)
		{
			return NearlyEqual(a, b, a - b);
		}

		/// <summary>
		/// Compare two vectors for near equality, using <see cref="NearlyEqual(double, double)"/> for comparison of individual coordinates.
		/// </summary>
		/// <param name="a">The first vector to compare.</param>
		/// <param name="b">The second vector to compare.</param>
		/// <returns>Whether the difference between all coordinates in the vector is within the expected error margin.</returns>
		public static bool NearlyEqual(Vec4D a, Vec4D b)
		{
			Vec4D delta = a - b;
			return NearlyEqual(a.X, b.X, delta.X) && NearlyEqual(a.Y, b.Y, delta.Y) && NearlyEqual(a.Z, b.Z, delta.Z) && NearlyEqual(a.W, b.W, delta.W);
		}

		[Conditional("TRACE")]
		public static void Assert(bool value, string error)
		{
			if (!value)
				throw new Exception(error);
		}

		[Conditional("TRACE")]
		public static void AssertEqual<T>(T a, T b, string error)
		{
			Assert(a.Equals(b), string.Format(error, a, b));
		}

		[Conditional("TRACE")]
		public static void AssertNearlyEqual(double a, double b, double allowed, string error)
		{
			Assert(Math.Abs(a - b) <= allowed, $"{error} {a} ~= {b} ({Math.Abs(a - b)})");
		}

		[Conditional("TRACE")]
		public static void AssertNearlyEqual(double a, double b, string error)
		{
			Assert(NearlyEqual(a, b), $"{error} {a} ~= {b} ({Math.Abs(a - b)})");
		}

		[Conditional("TRACE")]
		public static void AssertNearlyEqual(Vec4D a, Vec4D b, double allowed, string error)
		{
			Assert(Math.Abs((a - b).SquaredLength) > allowed * allowed,
				$"{error} {a} ~= {b} ({Math.Abs((a - b).Length)} > {allowed})");
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
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static double Clamp(double value, double minimum, double maximum)
		{
			if (Sse2.IsSupported)
			{
				Vector128<double> result = Vector128.CreateScalarUnsafe(value);
				result = Sse2.MaxScalar(result, Vector128.CreateScalarUnsafe(minimum));
				result = Sse2.MinScalar(result, Vector128.CreateScalarUnsafe(maximum));
				return result.ToScalar();
			}
			else
			{
				// Not using Math.Min/Max to keep behavior consistent with intrinsics
				if (value < minimum)
					return minimum;
				if (value > maximum)
					return maximum;
				return value;
			}
		}

		/// <summary>
		/// Linearly interpolates between <paramref name="minimum"/> and <paramref name="maximum"/> by the <paramref name="value"/> normally in range 0.0-1.0.
		/// </summary>
		/// <param name="value">The value, normally ranging from 0.0 to 1.0, to interpolate by.</param>
		/// <param name="minimum">The final value corresponding <paramref name="value"/>=0.0.</param>
		/// <param name="maximum">The final value corresponding <paramref name="value"/>=1.0.</param>
		/// <returns>The interpolated value, not constrained be between the <paramref name="minimum"/> and <paramref name="maximum"/>.</returns>
		public static double Lerp(double value, double minimum, double maximum)
		{
			return minimum + (maximum - minimum) * value;
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

		private static void MergeSortSplit<T>(T[] source, Comparer<T> comparer, int start, int end, T[] destination)
		{
			if (end - start <= 1)
				return;

			int mid = (start + end) / 2;
			MergeSortSplit(destination, comparer, start, mid, source);
			MergeSortSplit(destination, comparer, mid, end, source);
			MergeSortCopy(source, comparer, start, mid, end, destination);
		}

		private static void MergeSortCopy<T>(T[] source, Comparer<T> comparer, int start, int mid, int end, T[] destination)
		{
			int left = start, right = mid;

			for (int dest = start; dest < end; dest++)
			{
				if (left < mid && (right >= end || comparer.Compare(source[left], source[right]) <= 0))
				{
					destination[dest] = source[left];
					left++;
				}
				else
				{
					destination[dest] = source[right];
					right++;
				}
			}
		}

		/// <summary>
		/// Performs a merge sort on the array.
		/// This will maintain a stable order among the elements with equal comparisons.
		/// 
		/// <para>Best used to sort arrays of data that will usually be significantly out of order.</para>
		/// </summary>
		/// <typeparam name="T">The type of the array element.</typeparam>
		/// <param name="array">The array to sort in place.</param>
		/// <param name="comparer">The comparer to determine ordering.</param>
		public static void MergeSort<T>(T[] array, Comparer<T> comparer)
		{
			T[] copy = new T[array.Length];
			Array.Copy(array, copy, array.Length);
			MergeSortSplit(copy, comparer, 0, array.Length, array);
		}

		/// <summary>
		/// Performs a merge sort on the array using the default comparer for <typeparamref name="T"/>.
		/// This will maintain a stable order among the elements with equal comparisons.
		/// 
		/// <para>Best used to sort arrays of data that will usually be significantly out of order.</para>
		/// </summary>
		/// <typeparam name="T">The type of the array element.</typeparam>
		/// <param name="array">The array to sort in place.</param>
		public static void MergeSort<T>(T[] array)
		{
			MergeSort(array, Comparer<T>.Default);
		}

		/// <summary>
		/// Performs an insertion sort on the array.
		/// This will maintain a stable order among the elements with equal comparisons.
		/// 
		/// <para>Best used to sort short arrays, or arrays likely to be nearly ordered.</para>
		/// </summary>
		/// <typeparam name="T">The type of the array element.</typeparam>
		/// <param name="array">The array to sort in place.</param>
		/// <param name="comparer">The comparer to determine ordering.</param>
		public static void InsertSort<T>(IList<T> array, Comparer<T> comparer)
		{
			int size = array.Count;

			for (int i = 1; i < size; i++)
			{
				T a = array[i];
				T b;
				int j = i - 1;

				while (j >= 0 && comparer.Compare(a, b = array[j]) < 0)
				{
					array[j + 1] = b;
					j--;
				}

				array[j + 1] = a;
			}
		}

		/// <summary>
		/// Performs an insertion sort on the array using the default comparer for <typeparamref name="T"/>.
		/// This will maintain a stable order among the elements with equal comparisons.
		/// 
		/// <para>Best used to sort short arrays, or arrays likely to be nearly ordered.</para>
		/// </summary>
		/// <typeparam name="T">The type of the array element.</typeparam>
		/// <param name="array">The array to sort in place.</param>
		public static void InsertSort<T>(IList<T> array)
		{
			InsertSort(array, Comparer<T>.Default);
		}
	}
}
