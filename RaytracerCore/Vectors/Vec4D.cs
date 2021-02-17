using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RaytracerCore.Vectors
{
	/// <summary>
	/// A 4-dimensional vector using double precision floats.
	/// 
	/// <para>Includes an implementation of SIMD operations, but due to the conversion from and to AVX vectors, these are very slow.
	/// Prefer manual low level SIMD algorithm implementations with scalar implementations as backups.</para>
	/// </summary>
	[StructLayout(LayoutKind.Sequential, Pack = 0, Size = 32)]
	public readonly struct Vec4D
	{
		// Disabled since conversion from Vec4D to AVX vectors is slower than the improvement in individual operations
		public static readonly bool SIMD = false;
		public static readonly bool SIMDArithmetic = SIMD;
		public static readonly bool SIMDDot = SIMD;
		public static readonly bool SIMDCross = false;
		public static readonly bool SIMDLength = SIMD;
		public static readonly bool SIMDNormalize = true;	// compound operations for normalize should provide some speedup
		public static readonly bool SIMDEquals = false;

		/// <summary>
		/// Creates a vector on the horizontal between the <paramref name="vector"/> and a vertical vector,
		/// while defaulting to (1, 0, 0, 0) if the result would otherwise be zero-length.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4D CreateHorizontal(Vec4D vector)
		{
			// Cross against a (arbitrary) up vector to create horizontal vector
			Vec4D cross = vector.Cross(new Vec4D(0, 0, 1, 0));

			// If our up vector matches our input vector, return an arbitrary horizontal
			if (cross == new Vec4D(0, 0, 0, 0))
				return new Vec4D(1, 0, 0, 0);

			return cross.Normalize();
		}

		/// <summary>
		/// Create a new vector on the horizon that surrounds the <paramref name="pole"/> vector.
		/// </summary>
		/// <param name="pole">The pole at the center of the horizon.</param>
		/// <param name="z">The distance of the point along the pole.</param>
		/// <param name="theta">The angle of the point around the pole.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vec4D CreateHorizon(Vec4D pole, double z, double theta)
		{
			Vec4D cross = CreateHorizontal(pole);
			//double phi = Math.Acos(z);
			//return MatrixTransforms.Rotate(theta, pole) * MatrixTransforms.Rotate(phi, cross) * pole;
			return MatrixTransforms.Rotate(theta, pole) * ((pole * z) + (cross * Math.Sqrt(1 - z * z)));
		}

		public static Vec4D Zero => new Vec4D(0, 0, 0, 0);

		public readonly double X;
		public readonly double Y;
		public readonly double Z;
		public readonly double W;

		public Vec4D(double x, double y, double z, double w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator +(in Vec4D left, in Vec4D right)
		{
			if (SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* leftPtr = &left)
					fixed (Vec4D* rightPtr = &right)
					{
						Vector<double> packedA = Unsafe.Read<Vector<double>>(leftPtr);
						Vector<double> packedB = Unsafe.Read<Vector<double>>(rightPtr);
						Vector<double> result = Vector.Add(packedA, packedB);
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator -(in Vec4D left, in Vec4D right)
		{
			if (SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* thisPtr = &left)
					fixed (Vec4D* rightPtr = &right)
					{
						Vector<double> packedA = Unsafe.Read<Vector<double>>(thisPtr);
						Vector<double> packedB = Unsafe.Read<Vector<double>>(rightPtr);
						Vector<double> result = Vector.Subtract(packedA, packedB);
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator *(in Vec4D left, in Vec4D right)
		{
			if (Avx.IsSupported && SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* leftPtr = &left)
					fixed (Vec4D* rightPtr = &right)
					{
						Vector<double> packedA = Unsafe.Read<Vector<double>>(leftPtr);
						Vector<double> packedB = Unsafe.Read<Vector<double>>(rightPtr);
						Vector<double> result = Vector.Multiply(packedA, packedB);
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator -(in Vec4D value)
		{
			Util.Assert(value.W == 0, "Attempted to negate a non-directional vector.");

			if (Avx.IsSupported && SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* valuePtr = &value)
					{
						Vector256<double> packedA = Unsafe.Read<Vector256<double>>(valuePtr);
						Vector256<double> packedB = SIMDHelpers.BroadcastScalar4(-0D);
						Vector256<double> result = Avx.Xor(packedA, packedB);	
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(-value.X, -value.Y, -value.Z, -value.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator *(in Vec4D left, in double right)
		{
			if (Avx.IsSupported && SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* leftPtr = &left)
					fixed (double* rightPtr = &right)
					{
						Vector256<double> packed = Unsafe.Read<Vector256<double>>(leftPtr);
						Vector256<double> packedS = Avx.BroadcastScalarToVector256(rightPtr);
						Vector256<double> result = Avx.Multiply(packed, packedS);
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(left.X * right, left.Y * right, left.Z * right, left.W * right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator *(in double left, in Vec4D right)
		{
			return right * left;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator /(in Vec4D left, in Vec4D right)
		{
			if (SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* leftPtr = &left)
					fixed (Vec4D* rightPtr = &right)
					{
						Vector<double> packedA = Unsafe.Read<Vector<double>>(leftPtr);
						Vector<double> packedB = Unsafe.Read<Vector<double>>(&rightPtr);
						Vector<double> result = Vector.Divide(packedA, packedB);
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static Vec4D operator /(in Vec4D left, in double right)
		{
			if (SIMDArithmetic)
			{
				unsafe
				{
					fixed (Vec4D* leftPtr = &left)
					{
						Vector<double> packed = Unsafe.Read<Vector<double>>(leftPtr);
						Vector<double> packedS = new Vector<double>(right);
						Vector<double> result = Vector.Divide(packed, packedS);
						return Unsafe.Read<Vec4D>(Unsafe.AsPointer(ref result));
					}
				}
			}

			return new Vec4D(left.X / right, left.Y / right, left.Z / right, left.W / right);
		}

		/// <summary>
		/// Returns the squared length of the vector, including W. This should not be used on non-directional vectors.
		/// </summary>
		public double SquaredLength
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
			get
			{
				if (SIMDLength)
				{
					unsafe
					{
						fixed (Vec4D* thisPtr = &this)
						{
							return SIMDHelpers.LengthSquaredScalar(Unsafe.Read<Vector256<double>>(thisPtr));
						}
					}
				}

				return X * X + Y * Y + Z * Z + W * W;
			}
		}

		/// <summary>
		/// Returns the length of the vector, including the W value. This should not be used on non-directional vectors.
		/// </summary>
		public double Length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
			get
			{
				if (SIMDLength)
				{
					unsafe
					{
						fixed (Vec4D* thisPtr = &this)
						{
							return SIMDHelpers.LengthScalar(Unsafe.Read<Vector256<double>>(thisPtr));
						}
					}
				}

				return Math.Sqrt(SquaredLength);
			}
		}

		/// <summary>
		/// Creates a normalized vector. This should only be used on directional vectors.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public Vec4D Normalize()
		{
			if (SIMDNormalize)
			{
				unsafe
				{
					fixed (Vec4D* thisPtr = &this)
					{
						return (Vec4D)SIMDHelpers.Normalize(Unsafe.Read<Vector256<double>>(thisPtr));
					}
				}
			}

			return this / Length;
		}

		/// <summary>
		/// Perform a dot product.
		/// </summary>
		/// <param name="right">The right operand</param>
		/// <returns>A new <see cref="Vec4D"/> of the resulting product.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public double Dot(in Vec4D right)
		{
			if (SIMDDot)
				return SIMDHelpers.Dot((Vector256<double>)this, (Vector256<double>)right).ToScalar();

			return X * right.X + Y * right.Y + Z * right.Z + W * right.W;
		}

		/// <summary>
		/// Perform a cross product.
		/// </summary>
		/// <param name="right">The right operand</param>
		/// <returns>A new <see cref="Vec4D"/> of the resulting product.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vec4D Cross(in Vec4D right)
		{
			if (SIMDCross)
				return (Vec4D)SIMDHelpers.Cross((Vector256<double>)this, (Vector256<double>)right);

			return new Vec4D(Y * right.Z - Z * right.Y,
					Z * right.X - X * right.Z,
					X * right.Y - Y * right.X,
					0);
		}

		/// <summary>
		/// Whether this vector is valid (contains no NaN or infinity components).
		/// </summary>
		public bool Valid
		{
			get
			{
				return !double.IsNaN(X) &&
						!double.IsNaN(Y) &&
						!double.IsNaN(Z) &&
						!double.IsInfinity(X) &&
						!double.IsInfinity(Y) &&
						!double.IsInfinity(Z);
			}
		}

		/// <summary>
		/// Compare whether this vector nearly equals <paramref name="vec"/>.
		/// This method accounts for the expected error between the two vectors.
		/// </summary>
		public bool NearlyEquals(Vec4D vec)
		{
			return Util.NearlyEqual(SquaredLength, vec.SquaredLength, (this - vec).SquaredLength);
		}

		public override string ToString()
		{
			return $"{X:F3}, {Y:F3}, {Z:F3}, {W:F3}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
		public static unsafe explicit operator Vector256<double>(Vec4D vec)
		{
			// Load from the pointer so that we avoid as much reallocation in the generated assembly
			return Avx.LoadVector256((double*)&vec);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static unsafe explicit operator Vec4D(Vector256<double> vec)
		{
			// Convert directly from the memory representation of the AVX vector to avoid reallocation
			return Unsafe.Read<Vec4D>(&vec);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(in Vec4D a, in Vec4D b)
		{
			if (SIMDEquals)
			{
				unsafe
				{
					fixed (Vec4D* aPtr = &a)
					fixed (Vec4D* bPtr = &b)
					{
						Vector<double> packedA = Unsafe.Read<Vector<double>>(aPtr);
						Vector<double> packedB = Unsafe.Read<Vector<double>>(bPtr);
						return Vector.EqualsAll(packedA, packedB);
					}
				}
			}

			return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Vec4D a, Vec4D b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is Vec4D vecB)
				return this == vecB;

			return false;
		}
	}
}
