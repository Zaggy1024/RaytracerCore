using System.Drawing;

namespace RaytracerCore
{
	/// <summary>
	/// Double precision representation of RGB colors. Values range from 0 to 1, but are not clamped.
	/// </summary>
	public readonly struct DoubleColor
	{
		/// <summary>
		/// Create a <see cref="DoubleColor"/> from a .NET <see cref="Color"/>.
		/// </summary>
		/// <param name="color">The color to convert to a double precision color.</param>
		public static DoubleColor FromColor(Color color)
		{
			return new DoubleColor(color.R / 255.0, color.G / 255.0, color.B / 255.0);
		}

		public static readonly DoubleColor Placeholder = new DoubleColor(-1);
		public static readonly DoubleColor Black = new DoubleColor(0);

		/// <summary>
		/// The red component.
		/// </summary>
		public readonly double R;
		/// <summary>
		/// The green component.
		/// </summary>
		public readonly double G;
		/// <summary>
		/// The blue component.
		/// </summary>
		public readonly double B;

		/// <summary>
		/// Initialize a color with individual values for each component.
		/// </summary>
		/// <param name="r">The red value for the color.</param>
		/// <param name="g">The green value for the color.</param>
		/// <param name="b">The blue value for the color.</param>
		public DoubleColor(double r, double g, double b)
		{
			R = r;
			G = g;
			B = b;
		}

		/// <summary>
		/// Initialize a color with all components initialized to the same value.
		/// </summary>
		/// <param name="rgb">The value for all components.</param>
		public DoubleColor(double rgb) : this(rgb, rgb, rgb)
		{
		}

		public static DoubleColor operator +(DoubleColor left, DoubleColor right)
		{
			return new DoubleColor(left.R + right.R, left.G + right.G, left.B + right.B);
		}

		public static DoubleColor operator *(DoubleColor left, DoubleColor right)
		{
			return new DoubleColor(left.R * right.R, left.G * right.G, left.B * right.B);
		}

		public static DoubleColor operator *(DoubleColor left, double right)
		{
			return new DoubleColor(left.R * right, left.G * right, left.B * right);
		}

		public DoubleColor Clamp()
		{
			return new DoubleColor(Util.Clamp(R, 0, 1), Util.Clamp(G, 0, 1), Util.Clamp(B, 0, 1));
		}

		public static double GetLuminance(double r, double g, double b)
		{
			return 0.299 * r + 0.587 * g + 0.114 * b;
		}

		public double Luminance => GetLuminance(R, G, B);

		/// <summary>
		/// Converts the color to a .NET <see cref="Color"/>, using the specified alpha value.
		/// </summary>
		/// <param name="alpha">The alpha channel to use.</param>
		public Color ToColor(double alpha)
		{
			DoubleColor clamped = Clamp();
			alpha = Util.Clamp(alpha, 0, 1);
			return Color.FromArgb((int)(alpha * 255), (int)(clamped.R * 255), (int)(clamped.G * 255), (int)(clamped.B * 255));
		}

		/// <summary>
		/// Converts the color to a .NET <see cref="Color"/> at full opacity.
		/// </summary>
		public Color ToColor()
		{
			return ToColor(1);
		}

		/// <summary>
		/// Returns whether no component of this color is NaN or Infinity.
		/// </summary>
		public bool IsValid
		{
			get {
				return !double.IsNaN(R) &&
						!double.IsNaN(G) &&
						!double.IsNaN(B) &&
						!double.IsInfinity(R) &&
						!double.IsInfinity(G) &&
						!double.IsInfinity(B);
			}
		}

		public override int GetHashCode()
		{
			return ((int)(R * 0xFF) & 0xFF) << 24 +
					((int)(G * 0xFF) & 0xFF) << 16 +
					((int)(B * 0xFF) & 0xFF) << 8;
		}

		public override bool Equals(object other)
		{
			return other is DoubleColor && this == (DoubleColor)other;
		}

		public static bool operator ==(DoubleColor a, DoubleColor b)
		{
			return a.R == b.R && a.G == b.G && a.B == b.B;
		}

		public static bool operator !=(DoubleColor a, DoubleColor b)
		{
			return !(a == b);
		}

		public override string ToString()
		{
			return $"{R:0.##}, {G:0.##}, {B:0.##}";
		}
	}
}
