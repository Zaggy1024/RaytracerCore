using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Cameras
{
	/// <summary>
	/// Abstract class containing all the methods needed to define a camera.
	/// </summary>
	public abstract class Camera
	{
		public Vec4D initPosition;
		public Vec4D initLookAt;
		public Vec4D initUp;

		public Vec4D position;
		public Vec4D lookAt;
		public Vec4D up;

		protected double w2;
		protected double h2;
		protected Vec4D look;
		protected Vec4D side;

		public float exposure = 1;

		public double imagePlane;
		public double dofAmount;
		public double focalLength;

		public Camera(Vec4D pos, Vec4D lookAt, Vec4D up)
		{
			initPosition = pos;
			initLookAt = lookAt;
			initUp = up;

			Reset();
		}

		public Camera(Vec4D pos, Vec4D lookAt) : this(pos, lookAt, new Vec4D(0, 0, 1, 0))
		{

		}

		/// <summary>Resets this camera to its initial orientation.</summary>
		public virtual void Reset()
		{
			position = initPosition;
			lookAt = initLookAt;
			up = initUp;
		}

		/// <summary>Initializes the camera for a frame render.</summary>
		/// <param name="width">The width of the viewport in pixels</param>
		/// <param name="height">The height of the viewport in pixels</param>
		public virtual void InitRender(int width, int height)
		{
			w2 = width / 2D;
			h2 = height / 2D;
			// This code is a bit screwy to avoid flipping the camera, not sure if there's a better way to do this (yet)
			look = (lookAt - position).Normalize();
			side = look.Cross(-up).Normalize();
			up = look.Cross(side).Normalize();
			side = -side;
		}

		/// <summary>Gets a <see cref="Ray"/> for the specified pixel position in the viewport.</summary>
		/// <param name="x">The x position in partial pixels</param>
		/// <param name="y">The y position in partial pixels</param>
		/// <returns>A <see cref="Ray"/> originating from this camera, in the direction to be traced at the specified pixel.</returns>
		public abstract Ray GetRay(double x, double y);

		/// <summary>Rotates the camera around the specified <paramref name="axis"/> vector by <paramref name="angle"/> degrees.</summary>
		/// <param name="angle">The angle to rotate by.</param>
		/// <param name="axis">The axis to rotate around.</param>
		public void Rotate(double angle, Vec4D axis)
		{
			Mat4x4D rotMat = MatrixTransforms.Rotate(angle, axis.Normalize());
			position = rotMat * position;
			look = rotMat * look;
			up = rotMat * up;
		}
	}
}
