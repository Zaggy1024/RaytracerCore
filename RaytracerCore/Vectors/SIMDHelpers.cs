using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace RaytracerCore.Vectors
{
	/// <summary>
	/// Utility functions to assist in writing double-precision SIMD code.
	/// </summary>
	class SIMDHelpers
	{
		public static readonly bool Enabled = Avx2.IsSupported && Fma.IsSupported;

		/* 
		 * x = 0
		 * y = 1
		 * etc.
		 * 
		 * in reverse order, 2 bits per component
		*/
		public const byte XZYW8 = 0b_11_01_10_00;
		public const byte YZXW8 = 0b_11_00_10_01;
		public const byte ZXYW8 = 0b_11_01_00_10;

		private static unsafe double* GetPtr(Vector256<double> vec)
		{
			return (double*)&vec;
		}

		private static unsafe double* GetPtr(Vector128<double> vec)
		{
			return (double*)&vec;
		}

		/// <summary>
		/// Perform a cross product between two vectors.
		/// </summary>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector256<double> Cross(in Vector256<double> left, in Vector256<double> right)
		{
			Vector256<double> leftA;
			Vector256<double> leftB;
			Vector256<double> rightA;
			Vector256<double> rightB;

			// Swizzle our vectors to perform the subtraction and multiplication with the correct operands
			leftA = Avx2.Permute4x64(left, YZXW8);
			leftB = Avx2.Permute4x64(left, ZXYW8);
			rightA = Avx2.Permute4x64(right, ZXYW8);
			rightB = Avx2.Permute4x64(right, YZXW8);

			return Fma.MultiplySubtract(
				leftA, // *
				rightA,	// ) -
				Avx.Multiply(leftB, rightB));
		}

		/// <summary>
		/// Prepare a dot product into a two-component vector.
		/// </summary>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		/// <returns>A <see cref="Vector128{double}"/> of <see cref="double"/> containing the intermediate value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector256<double> PreDot(in Vector256<double> left, in Vector256<double> right)
		{
			Vector256<double> temp = Avx.Multiply(left, right);
			return Avx.HorizontalAdd(temp, temp);
		}

		/// <summary>
		/// Add the upper and lower halves of a 4-component vector together, resulting in a 2-component vector.
		/// <para>This is used to complete a dot product initiated in the <see cref="PreDot(in Vector256{double}, in Vector256{double})"/> function.</para>
		/// </summary>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		/// <returns>A <see cref="Vector128{double}"/> of <see cref="double"/> containing the added halves.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector128<double> Add2(in Vector256<double> hadd)
		{
			Vector128<double> upper = Avx.ExtractVector128(hadd, 1);
			return Sse2.Add(hadd.GetLower(), upper);
		}

		/// <summary>
		/// Perform a dot product of two vectors.
		/// </summary>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		/// <returns>A <see cref="Vector128{double}"/> with both <see cref="double"/> components set to the resulting dot product.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector128<double> Dot(in Vector256<double> left, in Vector256<double> right)
		{
			return Add2(PreDot(left, right));
		}

		/// <summary>
		/// Sum 4 4-component vectors into individual components of another 4-component vector.
		/// </summary>
		/// <param name="x">The vector to sum into the X output component.</param>
		/// <param name="y">The vector to sum into the Y output component.</param>
		/// <param name="z">The vector to sum into the Z output component.</param>
		/// <param name="w">The vector to sum into the W output component.</param>
		/// <returns>A <see cref="Vector256{double}"/> with each <see cref="double"/> component containing the sum of one input vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector256<double> Sum4(Vector256<double> x, Vector256<double> y, Vector256<double> z, Vector256<double> w)
		{
			// (x0+1)(y0+1)(x2+3)(y2+3)
			x = Avx.HorizontalAdd(x, y);
			// (z0+1)(w0+1)(z2+3)(w2+3)
			y = Avx.HorizontalAdd(z, w);

			// (x2+3)(y2+3)(z0+1)(w0+1)
			z = Avx.Permute2x128(x, y, 0b_00_10_00_01);
			// (x0+1)(y0+1)(z2+3)(w2+3)
			w = Avx.Blend(x, y, 0b1100);

			// (sum(x))(sum(y))(sum(z))(sum(w))
			return Avx.Add(z, w);
		}

		/// <summary>
		/// Sum a 4-component vector into each of the 4 components in the output.
		/// </summary>
		/// <param name="vector">The vector to sum.</param>
		/// <returns>A <see cref="Vector256{double}"/> with each <see cref="double"/> component containing the same summed value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector256<double> Sum4(Vector256<double> vector)
		{
			// (0+1)(0+1)(2+3)(2+3)
			vector = Avx.HorizontalAdd(vector, vector);

			// (2+3)(2+3)(0+1)(0+1)
			Vector256<double> b = Avx2.Permute4x64(vector, 0b01_00_11_10);

			// (sum())(sum())(sum())(sum())
			return Avx.Add(vector, b);
		}

		/// <summary>
		/// Sum 2 4-component vectors into the components of a 2-component vector.
		/// </summary>
		/// <param name="x">The vector to sum into the X component.</param>
		/// <param name="y">The vector to sum into the Y component.</param>
		/// <returns>A <see cref="Vector128{double}"/> with each <see cref="double"/> component containing the sum of one input vector.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector128<double> Sum2(Vector256<double> x, Vector256<double> y)
		{
			x = Avx.HorizontalAdd(x, y);
			return Add2(x);
		}

		/// <summary>
		/// Combine 2 2-component vectors into a 4-component vector.
		/// </summary>
		/// <param name="a">The lower half of the output vector.</param>
		/// <param name="b">The upper half of the output vector.</param>
		/// <returns>A <see cref="Vector256{double}"/> of <see cref="double"/> with the <paramref name="a"/> as the lower half and <paramref name="b"/> as the upper.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector256<double> CombinePacked2(in Vector128<double> a, in Vector128<double> b)
		{
			Vector256<double> result = a.ToVector256();
			return Avx.InsertVector128(result, b, 1);
		}

		/// <summary>
		/// Broadcast a scalar value into a 2-component vector.
		/// 
		/// <para>This should be slightly faster than <see cref="Vector128.Create(double)"/> due to the way the assembly is generated.
		/// However, use <see cref="Vector128{double}.Vector128()"/> to initialize a zeroed vector.</para>
		/// </summary>
		/// <param name="value">The value to initialize to.</param>
		/// <returns>A <see cref="Vector128{double}"/> with each component initialized to <paramref name="value"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector128<double> BroadcastScalar2(double value)
		{
			return Sse3.MoveAndDuplicate(Vector128.CreateScalarUnsafe(value));
		}

		/// <summary>
		/// Broadcast a scalar value into a 2-component vector.
		/// 
		/// <para>This is an alias to <see cref="Vector256.Create(double)"/> at the moment, but the assembly generated is not perfectly efficient.
		/// However, use <see cref="Vector256{double}.Vector256()"/> to initialize a zeroed vector.</para>
		/// </summary>
		/// <param name="value">The value to initialize to.</param>
		/// <returns>A <see cref="Vector128{double}"/> with each component initialized to <paramref name="value"/>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector256<double> BroadcastScalar4(double value)
		{
			return Vector256.Create(value);
		}

		/// <summary>
		/// Swap the components of a 2-component vector.
		/// </summary>
		/// <param name="vec2">The vector to swap the components of.</param>
		/// <returns>A <see cref="Vector128{double}"/> with the components swapped.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vector128<double> Swap(Vector128<double> vec2)
		{
			// Return the vector shuffled to YX, so we use a single instruction minimum
			return Avx.Permute(vec2, 0b01);
		}

		/// <summary>
		/// Multiply a vector by a matrix, represent by each row of the matrix.
		/// </summary>
		/// <param name="vector">The vector to multiply.</param>
		/// <param name="row0">The first row of the matrix.</param>
		/// <param name="row1">The second row of the matrix.</param>
		/// <param name="row2">The third row of the matrix.</param>
		/// <param name="row3">The fourth row of the matrix.</param>
		/// <returns>A <see cref="Vector256{double}"/> of <see cref="double"/> multiplied by the input matrix.</returns>
		public static Vector256<double> MultiplyMatrixVector(
			Vector256<double> vector,
			Vector256<double> row0,
			Vector256<double> row1,
			Vector256<double> row2,
			Vector256<double> row3)
		{
			// Premultiply our vectors
			row0 = Avx.Multiply(row0, vector);
			row1 = Avx.Multiply(row1, vector);
			row2 = Avx.Multiply(row2, vector);
			row3 = Avx.Multiply(row3, vector);

			// Sum all vectors into components of a vec4
			return Sum4(row0, row1, row2, row3);
		}

		/// <summary>
		/// Calculate the squared length of the input vector in each component of a 4-component vector.
		/// 
		/// <para>Formula: <c>X * X + Y * Y + Z * Z + W * W</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector to get the squared length of.</param>
		/// <returns>A <see cref="Vector256{double}"/> of <see cref="double"/> with each component containing the squared length of <paramref name="vector"/>.</returns>
		public static Vector256<double> LengthSquared4(Vector256<double> vector)
		{
			vector = Avx.Multiply(vector, vector);
			return Sum4(vector);
		}

		/// <summary>
		/// Calculate the length of the input vector in each component of a 4-component vector.
		/// 
		/// <para>Formula: <c>sqrt(X * X + Y * Y + Z * Z + W * W)</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector to get the length of.</param>
		/// <returns>A <see cref="Vector256{double}"/> of <see cref="double"/> with each component containing the length of <paramref name="vector"/>.</returns>
		public static Vector256<double> Length4(Vector256<double> vector)
		{
			return Avx.Sqrt(LengthSquared4(vector));
		}

		/// <summary>
		/// Calculate the squared length of the input vector in each component of a 2-component vector.
		/// 
		/// <para>Formula: <c>X * X + Y * Y + Z * Z + W * W</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector to get the squared length of.</param>
		/// <returns>A <see cref="Vector128{double}"/> of <see cref="double"/> with each component containing the squared length of <paramref name="vector"/>.</returns>
		public static Vector128<double> LengthSquared2(Vector256<double> vector)
		{
			vector = Avx.Multiply(vector, vector);
			return Sum2(vector, vector);
		}

		/// <summary>
		/// Calculate the length of the input vector in each component of a 2-component vector.
		/// 
		/// <para>Formula: <c>sqrt(X * X + Y * Y + Z * Z + W * W)</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector to get the length of.</param>
		/// <returns>A <see cref="Vector128{double}"/> of <see cref="double"/> with each component containing the length of <paramref name="vector"/>.</returns>
		public static Vector128<double> Length2(Vector256<double> vector)
		{
			return Sse2.Sqrt(LengthSquared2(vector));
		}

		/// <summary>
		/// Calculate the squared length of the input vector into a scalar <see cref="double"/>.
		/// 
		/// <para>Formula: <c>X * X + Y * Y + Z * Z + W * W</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector to get the squared length of.</param>
		/// <returns>The <see cref="double"/> squared length of <paramref name="vector"/>.</returns>
		public static double LengthSquaredScalar(Vector256<double> vector)
		{
			return LengthSquared2(vector).ToScalar();
		}

		/// <summary>
		/// Calculate the length of the input vector into a scalar <see cref="double"/>.
		/// 
		/// <para>Formula: <c>sqrt(X * X + Y * Y + Z * Z + W * W)</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector to get the length of.</param>
		/// <returns>The <see cref="double"/> length of <paramref name="vector"/>.</returns>
		public static double LengthScalar(Vector256<double> vector)
		{
			return Sse2.SqrtScalar(LengthSquared2(vector)).ToScalar();
		}

		/// <summary>
		/// Normalizes to 1-length.
		/// 
		/// <para>Formula: <c>v / sqrt(vX * vX + vY * vY + vZ * vZ + vW * vW)</c></para>
		/// 
		/// <para>Be aware, this will include the W component in the calculation, so only directional vectors should be used.</para>
		/// </summary>
		/// <param name="vector">The vector normalize.</param>
		/// <returns>A normalized <see cref="Vector256{double}"/> of <see cref="double"/>.</returns>
		public static Vector256<double> Normalize(Vector256<double> vector)
		{
			return Avx.Divide(vector, Length4(vector));
		}
	}
}
