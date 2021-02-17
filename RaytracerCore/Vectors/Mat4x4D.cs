using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RaytracerCore.Vectors
{
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct Mat4x4D
	{
		public static readonly Mat4x4D Identity4x4 = new Mat4x4D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			0, 0, 0, 1);

		public static readonly bool SIMD = true;

		public readonly double D00;
		public readonly double D01;
		public readonly double D02;
		public readonly double D03;

		public readonly double D10;
		public readonly double D11;
		public readonly double D12;
		public readonly double D13;

		public readonly double D20;
		public readonly double D21;
		public readonly double D22;
		public readonly double D23;

		public readonly double D30;
		public readonly double D31;
		public readonly double D32;
		public readonly double D33;

		public Mat4x4D(double d00, double d01, double d02, double d03,
			double d10, double d11, double d12, double d13,
			double d20, double d21, double d22, double d23,
			double d30, double d31, double d32, double d33)
		{
			D00 = d00;
			D01 = d01;
			D02 = d02;
			D03 = d03;

			D10 = d10;
			D11 = d11;
			D12 = d12;
			D13 = d13;

			D20 = d20;
			D21 = d21;
			D22 = d22;
			D23 = d23;

			D30 = d30;
			D31 = d31;
			D32 = d32;
			D33 = d33;
		}

		/// <summary>Create a transposed version of the matrix.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Mat4x4D Transpose()
		{
			return new Mat4x4D(
					D00, D10, D20, D30,
					D01, D11, D21, D31,
					D02, D12, D22, D32,
					D03, D13, D23, D33);
		}

		/// <summary>Create a transposed version of the matrix, but setting row 3 and column 3 to identity, removing translation.</summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Mat4x4D Transpose3x3()
		{
			return new Mat4x4D(
					D00,	D10,	D20,	0,
					D01,	D11,	D21,	0,
					D02,	D12,	D22,	0,
					0,		0,		0,		1);
		}

		public static Mat4x4D operator +(in Mat4x4D left, in Mat4x4D right)
		{
			return new Mat4x4D(
					left.D00 + right.D00, left.D01 + right.D01, left.D02 + right.D02, left.D03 + right.D03,
					left.D10 + right.D10, left.D11 + right.D11, left.D12 + right.D12, left.D13 + right.D13,
					left.D20 + right.D20, left.D21 + right.D21, left.D22 + right.D22, left.D23 + right.D23,
					left.D30 + right.D30, left.D31 + right.D31, left.D32 + right.D32, left.D33 + right.D33);
		}

		public static Mat4x4D operator *(in Mat4x4D left, in Mat4x4D right)
		{
			if (SIMD)
			{
				unsafe
				{
					fixed (double* leftPtr = &left.D00)
					{
						Mat4x4D bRot = right.Transpose();
						double* rightPtr = &bRot.D00;
						Mat4x4D result = new Mat4x4D();
						double* resultVals = &result.D00;

						for (int x = 0; x < 4; x++)
						{
							for (int y = 0; y < 4; y++)
							{
								Vector<double> simdVec1 = Unsafe.Read<Vector<double>>(&leftPtr[y * 4]);
								Vector<double> simdVec2 = Unsafe.Read<Vector<double>>(&rightPtr[x * 4]);
								resultVals[y * 4 + x] = Vector.Dot(simdVec1, simdVec2);
							}
						}

						return result;
					}
				}
			}

			return new Mat4x4D(
					left.D00 * right.D00 + left.D01 * right.D10 + left.D02 * right.D20 + left.D03 * right.D30,
					left.D00 * right.D01 + left.D01 * right.D11 + left.D02 * right.D21 + left.D03 * right.D31,
					left.D00 * right.D02 + left.D01 * right.D12 + left.D02 * right.D22 + left.D03 * right.D32,
					left.D00 * right.D03 + left.D01 * right.D13 + left.D02 * right.D23 + left.D03 * right.D33,

					left.D10 * right.D00 + left.D11 * right.D10 + left.D12 * right.D20 + left.D13 * right.D30,
					left.D10 * right.D01 + left.D11 * right.D11 + left.D12 * right.D21 + left.D13 * right.D31,
					left.D10 * right.D02 + left.D11 * right.D12 + left.D12 * right.D22 + left.D13 * right.D32,
					left.D10 * right.D03 + left.D11 * right.D13 + left.D12 * right.D23 + left.D13 * right.D33,

					left.D20 * right.D00 + left.D21 * right.D10 + left.D22 * right.D20 + left.D23 * right.D30,
					left.D20 * right.D01 + left.D21 * right.D11 + left.D22 * right.D21 + left.D23 * right.D31,
					left.D20 * right.D02 + left.D21 * right.D12 + left.D22 * right.D22 + left.D23 * right.D32,
					left.D20 * right.D03 + left.D21 * right.D13 + left.D22 * right.D23 + left.D23 * right.D33,

					left.D30 * right.D00 + left.D31 * right.D10 + left.D32 * right.D20 + left.D33 * right.D30,
					left.D30 * right.D01 + left.D31 * right.D11 + left.D32 * right.D21 + left.D33 * right.D31,
					left.D30 * right.D02 + left.D31 * right.D12 + left.D32 * right.D22 + left.D33 * right.D32,
					left.D30 * right.D03 + left.D31 * right.D13 + left.D32 * right.D23 + left.D33 * right.D33);
		}

		/// <summary>Transform the provided SIMD <paramref name="vector"/> by the <paramref name="matrix"/>.</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static unsafe Vector256<double> TransformVec4(in Mat4x4D matrix, in Vector256<double> vector)
		{
			Vector256<double> row0;
			Vector256<double> row1;
			Vector256<double> row2;
			Vector256<double> row3;

			// Read out our values into the registers but unpin asap
			fixed (double* matPtr = &matrix.D00)
			{
				row0 = Avx.LoadVector256(matPtr);
				row1 = Avx.LoadVector256(&matPtr[4]);
				row2 = Avx.LoadVector256(&matPtr[8]);
				row3 = Avx.LoadVector256(&matPtr[12]);
			}

			return SIMDHelpers.MultiplyMatrixVector(vector, row0, row1, row2, row3);
		}

		public static Vec4D operator *(in Mat4x4D left, in Vec4D right)
		{
			if (Avx2.IsSupported)
				return (Vec4D)TransformVec4(left, (Vector256<double>)right);

			return new Vec4D(
					left.D00 * right.X + left.D01 * right.Y + left.D02 * right.Z + left.D03 * right.W,
					left.D10 * right.X + left.D11 * right.Y + left.D12 * right.Z + left.D13 * right.W,
					left.D20 * right.X + left.D21 * right.Y + left.D22 * right.Z + left.D23 * right.W,
					left.D30 * right.X + left.D31 * right.Y + left.D32 * right.Z + left.D33 * right.W);
		}

		/// <summary>Whether this matrix is valid (contains no NaN or infinity components).</summary>
		public bool IsValid
		{
			get
			{
				if (double.IsNaN(D00) || double.IsInfinity(D00)) return false;
				if (double.IsNaN(D01) || double.IsInfinity(D01)) return false;
				if (double.IsNaN(D02) || double.IsInfinity(D02)) return false;
				if (double.IsNaN(D03) || double.IsInfinity(D03)) return false;

				if (double.IsNaN(D10) || double.IsInfinity(D10)) return false;
				if (double.IsNaN(D11) || double.IsInfinity(D11)) return false;
				if (double.IsNaN(D12) || double.IsInfinity(D12)) return false;
				if (double.IsNaN(D13) || double.IsInfinity(D13)) return false;

				if (double.IsNaN(D20) || double.IsInfinity(D20)) return false;
				if (double.IsNaN(D21) || double.IsInfinity(D21)) return false;
				if (double.IsNaN(D22) || double.IsInfinity(D22)) return false;
				if (double.IsNaN(D23) || double.IsInfinity(D23)) return false;

				if (double.IsNaN(D30) || double.IsInfinity(D30)) return false;
				if (double.IsNaN(D31) || double.IsInfinity(D31)) return false;
				if (double.IsNaN(D32) || double.IsInfinity(D32)) return false;
				if (double.IsNaN(D33) || double.IsInfinity(D33)) return false;

				return true;
			}
		}

		public override string ToString()
		{
			return $@"{D00}, {D01}, {D02}, {D03},\n
					{D10}, {D11}, {D12}, {D13},\n
					{D20}, {D21}, {D22}, {D23},\n
					{D30}, {D31}, {D32}, {D33}";
		}

		public static bool operator ==(in Mat4x4D left, in Mat4x4D right)
		{
			return left.D00 == right.D00 && left.D01 == right.D01 && left.D02 == right.D02 && left.D03 == right.D03 &&
					left.D10 == right.D10 && left.D11 == right.D11 && left.D12 == right.D12 && left.D13 == right.D13 &&
					left.D20 == right.D20 && left.D21 == right.D21 && left.D22 == right.D22 && left.D23 == right.D23 &&
					left.D30 == right.D30 && left.D31 == right.D31 && left.D32 == right.D32 && left.D33 == right.D33;
		}

		public static bool operator !=(in Mat4x4D left, in Mat4x4D right)
		{
			return !(left == right);
		}

		public override bool Equals(object obj)
		{
			if (obj is Mat4x4D mat)
				return this == mat;

			return false;
		}

		public override int GetHashCode()
		{
			return D00.GetHashCode() ^ D01.GetHashCode() ^ D02.GetHashCode() ^ D03.GetHashCode() ^
				D10.GetHashCode() ^ D11.GetHashCode() ^ D12.GetHashCode() ^ D13.GetHashCode() ^
				D20.GetHashCode() ^ D21.GetHashCode() ^ D22.GetHashCode() ^ D23.GetHashCode() ^
				D30.GetHashCode() ^ D31.GetHashCode() ^ D32.GetHashCode() ^ D33.GetHashCode();
		}
	}
}
