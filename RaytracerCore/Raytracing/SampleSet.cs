using System;

namespace RaytracerCore.Raytracing
{
	/// <summary>
	/// Stores information about the samples performed in a pixel of a raytracer.
	/// </summary>
	public class SampleSet
	{
		public DoubleColor Color { get; private set; }
		public uint Samples { get; private set; }
		public uint Misses { get; private set; }

		public SampleSet(DoubleColor color, uint samples, uint misses)
		{
			Color = color;
			Samples = samples;
			Misses = misses;
		}

		public SampleSet()
		{
			Color = DoubleColor.Black;
			Samples = 0;
			Misses = 0;
		}

		/// <summary>
		/// Adds a hit sample to the pixel.
		/// </summary>
		/// <param name="sample">The color to be added.</param>
		public void AddSample(DoubleColor sample)
		{
			Color += sample;
			Samples++;
		}

		/// <summary>
		/// Adds a missed sample to the pixel.
		/// </summary>
		public void AddMiss()
		{
			Misses++;
		}

		// Convert a double color to a 32-bit ARGB color code.
		private int GetColorCode(double r, double g, double b, double a)
		{
			return ((int)(Util.Clamp(a, 0, 1) * 255) << 24) |
					((int)(Util.Clamp(r, 0, 1) * 255) << 16) |
					((int)(Util.Clamp(g, 0, 1) * 255) << 8) |
					((int)(Util.Clamp(b, 0, 1) * 255) << 0);
		}

		/// <summary>
		/// Calculate the final color output for a pixel.
		/// </summary>
		/// <param name="back">The background color.</param>
		/// <param name="backA">The background alpha value.</param>
		/// <param name="exposure">The exposure used to brighten the image.</param>
		public int GetOutput(DoubleColor back, double backA, double exposure)
		{
			if (Samples == 0)
				return GetColorCode(back.R * exposure, back.G * exposure, back.B * exposure, backA);

			/* Attempt at making transparent misses work correctly,
			currently output is way too dark.
			
			double total = samples + misses;
			//double colorMult = exposure / samples;
			double transparent = 1 - backA;
			double colorMult = exposure / (samples + (misses * transparent));

			double r = color.R * colorMult;
			double g = color.G * colorMult;
			double b = color.B * colorMult;
			double a = 1;

			double backAlphaAmt = misses / total;
			double backAmt = backAlphaAmt * backA;
			backAlphaAmt *= 1 - Math.Min(DoubleColor.GetLuminance(r, g, b) * transparent, 1);

			r += (back.R - r) * backAmt;
			g += (back.G - g) * backAmt;
			b += (back.B - b) * backAmt;
			a += (backA - a) * backAlphaAmt;*/

			double total = Samples + Misses;
			double colorMult = exposure / Samples;

			double r = Color.R * colorMult;
			double g = Color.G * colorMult;
			double b = Color.B * colorMult;
			double a = 1;

			double backAlphaAmt = Misses / total;
			double backAmt = backAlphaAmt * backA;

			r += (back.R - r) * backAmt;
			g += (back.G - g) * backAmt;
			b += (back.B - b) * backAmt;
			a += (backA - a) * backAlphaAmt;

			const double gamma = 1 / 2.2;
			r = Math.Pow(r, gamma);
			g = Math.Pow(g, gamma);
			b = Math.Pow(b, gamma);

			return GetColorCode(r,
				g,
				b,
				a);
		}

		/// <summary>
		/// Calculate the final color output for a pixel and convert to a .NET <see cref="System.Drawing.Color"/>.
		/// </summary>
		/// <param name="back">The background color.</param>
		/// <param name="backA">The background alpha value.</param>
		/// <param name="exposure">The exposure used to brighten the image.</param>
		public System.Drawing.Color GetColor(DoubleColor back, double backA, double exposure)
		{
			return System.Drawing.Color.FromArgb(GetOutput(back, backA, exposure));
		}
	}
}
