using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Primitives
{
	public class Sphere : Primitive
	{
		public Vec4D Center;

		protected double RadiusValue;
		protected double RadiusSqr;

		bool Transformed;
		Mat4x4D MatrixToObject = Mat4x4D.Identity4x4;
		Mat4x4D MatrixToWorld = Mat4x4D.Identity4x4;
		Mat4x4D MatrixToNormal = Mat4x4D.Identity4x4;
		protected Vec4D CenterObj;

		public Sphere(Vec4D center, double radius)
		{
			Center = center;
			Radius = radius;
		}
		
		public override void Transform(Mat4x4D forward, Mat4x4D inverse)
		{
			if (forward != Mat4x4D.Identity4x4)
				Transformed = true;
			MatrixToObject = MatrixToObject * forward;
			MatrixToWorld = inverse * MatrixToWorld;
			MatrixToNormal = MatrixToWorld.Transpose3x3();
			CenterObj = MatrixToWorld * Center;
		}

		public double Radius
		{
			get => RadiusValue;
			set
			{
				RadiusValue = value;
				RadiusSqr = value * value;
			}
		}

		/// <summary>Perform a ray trace using AVX2 and FMA instructions. This is architecture-specific.</summary>
		private unsafe Hit[] RayTraceAVX(Ray ray)
		{
			Vector256<double> objOrigin = (Vector256<double>)ray.Origin;
			Vector256<double> objDir = (Vector256<double>)ray.Direction;
			Vector256<double> worldOrigin = objOrigin;
			Vector256<double> worldDir = objDir;
			Vector256<double> center = (Vector256<double>)Center;

			if (Transformed)
			{
				Vector256<double> matInv0, matInv1, matInv2, matInv3;

				unsafe
				{
					fixed (double* ptr = &MatrixToWorld.D00)
					{
						matInv0 = Avx.LoadVector256(ptr);
						matInv1 = Avx.LoadVector256(&ptr[4]);
						matInv2 = Avx.LoadVector256(&ptr[8]);
						matInv3 = Avx.LoadVector256(&ptr[12]);
					}
				}

				objOrigin = SIMDHelpers.MultiplyMatrixVector(objOrigin, matInv0, matInv1, matInv2, matInv3);
				objDir = SIMDHelpers.Normalize(SIMDHelpers.MultiplyMatrixVector(objDir, matInv0, matInv1, matInv2, matInv3));
			}

			// Prepare dots in vec4 form
			Vector256<double> offset = Avx.Subtract(objOrigin, center);
			Vector256<double> b4 = SIMDHelpers.PreDot(offset, objDir);
			Vector256<double> offSqr4 = SIMDHelpers.PreDot(offset, offset);

			// Do the scalar math before we jump back into SIMD vectors
			double b = -2 * SIMDHelpers.Add2(b4).ToScalar();
			double c = SIMDHelpers.Add2(offSqr4).ToScalar() - RadiusSqr;
			double radix = Math.Sqrt((b * b) - (4 * c));

			// Get the hit distances from the calculated values above (NaN if miss, negative if hit is behind the ray origin)
			double distFar = (b + radix) / 2;
			double distClose = (b - radix) / 2;

			// Calculate which hits to return, and their position, normals, etc
			Vector256<double> radius4 = SIMDHelpers.BroadcastScalar4(RadiusValue);
			Vector256<double> positionFar = Fma.MultiplyAdd(SIMDHelpers.BroadcastScalar4(distFar), objDir, objOrigin);
			Vector256<double> normalFar = Avx.Divide(Avx.Subtract(positionFar, center), radius4);

			Vector256<double> positionClose = Fma.MultiplyAdd(SIMDHelpers.BroadcastScalar4(distClose), objDir, objOrigin);
			Vector256<double> normalClose = Avx.Divide(Avx.Subtract(positionClose, center), radius4);

			if (Transformed)
			{
				// Load world to object matrix into vectors
				Vector256<double> matFw0, matFw1, matFw2, matFw3;

				unsafe
				{
					fixed (double* ptr = &MatrixToObject.D00)
					{
						matFw0 = Avx.LoadVector256(ptr);
						matFw1 = Avx.LoadVector256(&ptr[4]);
						matFw2 = Avx.LoadVector256(&ptr[8]);
						matFw3 = Avx.LoadVector256(&ptr[12]);
					}
				}

				positionFar = SIMDHelpers.MultiplyMatrixVector(positionFar, matFw0, matFw1, matFw2, matFw3);
				positionClose = SIMDHelpers.MultiplyMatrixVector(positionClose, matFw0, matFw1, matFw2, matFw3);

				// Load normal matrix into vectors
				Vector256<double> normMat0, normMat1, normMat2, normMat3;

				unsafe
				{
					fixed (double* ptr = &MatrixToNormal.D00)
					{
						normMat0 = Avx.LoadVector256(ptr);
						normMat1 = Avx.LoadVector256(&ptr[4]);
						normMat2 = Avx.LoadVector256(&ptr[8]);
						normMat3 = Avx.LoadVector256(&ptr[12]);
					}
				}

				normalFar = SIMDHelpers.Normalize(SIMDHelpers.MultiplyMatrixVector(normalFar, normMat0, normMat1, normMat2, normMat3));
				//distFar = Math.CopySign(SIMDHelpers.LengthScalar(Avx.Subtract(positionFar, worldOrigin)), distFar);
				distFar = SIMDHelpers.Dot(worldDir, Avx.Subtract(positionFar, worldOrigin)).ToScalar();

				normalClose = SIMDHelpers.Normalize(SIMDHelpers.MultiplyMatrixVector(normalClose, normMat0, normMat1, normMat2, normMat3));
				//distClose = Math.CopySign(SIMDHelpers.LengthScalar(Avx.Subtract(positionClose, worldOrigin)), distClose);
				distClose = SIMDHelpers.Dot(worldDir, Avx.Subtract(positionClose, worldOrigin)).ToScalar();
			}

			// Invert the far normal to point inwards
			normalFar = Avx.Xor(normalFar, SIMDHelpers.BroadcastScalar4(-0D));

			// Not greater than to exclude NaNs
			if (!(distFar >= 0))
				return default;

			if (!(distClose >= 0))
				return new Hit[] { new Hit(this, (Vec4D)positionFar, distFar, (Vec4D)normalFar, true) };

			return new Hit[] {
				new Hit(this, (Vec4D)positionClose, distClose, (Vec4D)normalClose, false),
				new Hit(this, (Vec4D)positionFar, distFar, (Vec4D)normalFar, true)
			};
		}

		private Hit GetHit(Ray worldRay, Ray objectRay, double distance, bool inside)
		{
			Vec4D position = objectRay.GetPoint(distance);
			Vec4D normal = (position - Center) / Radius;

			if (Transformed)
			{
				position = MatrixToObject * position;
				normal = (MatrixToNormal * normal).Normalize();
				//distance = (position - worldRay.Start).Length;
				distance = worldRay.Direction.Dot(position - worldRay.Origin);
			}

			if (inside)
				normal = -normal;

			return new Hit(this, position, distance, normal, inside);
		}

		internal override Hit[] DoRayTrace(Ray ray)
		{
			// Use AVX if our instructions should be usable on our current architecture.
			if (SIMDHelpers.Enabled)
				return RayTraceAVX(ray);

			Ray objectRay = ray;

			if (Transformed)
				objectRay = ray.Transform(MatrixToWorld);

			Vec4D offset = objectRay.Origin - Center;
			double b = -2 * offset.Dot(objectRay.Direction);
			double c = offset.SquaredLength - RadiusSqr;
			double radix = Math.Sqrt((b * b) - (4 * c));

			// Exit if we're not going to have any hits
			// NaN radix = negative discriminant, ray missed
			// radix < -b means that the ray origin is past the farther intersection
			// Invert the condition to check for NaN in less instructions
			if (!(radix >= -b))
				return default;

			// If radix < b, closest intersection is ahead of the ray, return two hits
			if (radix < b)
			{
				return new Hit[] {
					GetHit(ray, objectRay, (b - radix) / 2, false),
					GetHit(ray, objectRay, (b + radix) / 2, true)
				};
			}

			// Return one hit if we're inside the sphere
			return new Hit[] { GetHit(ray, objectRay, (b + radix) / 2, true) };
		}
	
	}
}
