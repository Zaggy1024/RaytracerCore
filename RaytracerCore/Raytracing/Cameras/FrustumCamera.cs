using System;

using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Cameras
{
	public class FrustumCamera : Camera
	{
		public double fovY;

		protected double tanFOVY2;
		protected double tanFOVX2;

		public FrustumCamera(Vec4D pos, Vec4D lookAt, Vec4D up, double fovYDeg) : base(pos, lookAt, up)
		{
			fovY = Consts.toRadians(fovYDeg);
		}

		public FrustumCamera(Vec4D pos, Vec4D lookAt, double fovYDeg) : base(pos, lookAt)
		{
			fovY = Consts.toRadians(fovYDeg);
		}

		public override void InitRender(int w, int h)
		{
			base.InitRender(w, h);

			tanFOVY2 = Math.Tan(fovY / 2);
			tanFOVX2 = tanFOVY2 * (w / (double)h);
			tanFOVY2 = -tanFOVY2;
		}

		public override Ray GetRay(double x, double y)
		{
			double offX = tanFOVX2 * ((x - w2) / w2);
			double offY = tanFOVY2 * ((y - h2) / h2);

			Vec4D dir = look + (side * offX) + (up * offY);

			return Ray.Directional(position, dir);
		}
	}
}
