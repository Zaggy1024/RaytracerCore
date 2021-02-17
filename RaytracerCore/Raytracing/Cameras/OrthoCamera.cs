using RaytracerCore.Vectors;

namespace RaytracerCore.Raytracing.Cameras
{
	public class OrthoCamera : Camera
	{
		public double sizeMult;

		protected double hMult;
		protected double vMult;

		public OrthoCamera(Vec4D pos, Vec4D lookAt, double size) : base(pos, lookAt)
		{
			sizeMult = size;
		}

		public OrthoCamera(Vec4D pos, Vec4D lookAt, Vec4D up, double size) : base(pos, lookAt, up)
		{
			sizeMult = size;
		}

		public override void InitRender(int w, int h)
		{
			base.InitRender(w, h);

			double camW = (1 / w2);
			double camH = (1 / h2) * (h / (double)w);

			hMult = camW * sizeMult;
			vMult = -camH * sizeMult;
		}

		public override Ray GetRay(double x, double y)
		{
			Vec4D start = position + (side * ((x - w2) * hMult)) + (up * ((y - h2) * vMult));

			return Ray.Directional(start, look);
		}
	}
}
