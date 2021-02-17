using System;
using System.Drawing;

namespace RaytracerCore.Raytracing
{
	/// <summary>
	/// Stores information about the samples performed in a pixel of a raytracer.
	/// </summary>
	public readonly struct SampleSet
	{
		public readonly DoubleColor color;
		public readonly uint samples;
		public readonly uint misses;

		public SampleSet(DoubleColor color, uint samples, uint misses)
		{
			this.color = color;
			this.samples = samples;
			this.misses = misses;
		}

		/// <summary>
		/// Adds a hit sample to the pixel.
		/// </summary>
		/// <param name="sample">The color to be added.</param>
		public SampleSet AddSample(DoubleColor sample)
		{
			return new SampleSet(color + sample, samples + 1, misses);
		}

		/// <summary>
		/// Adds a missed sample to the pixel.
		/// </summary>
		public SampleSet AddMiss()
		{
			return new SampleSet(color, samples, misses + 1);
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
			if (samples == 0)
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

			double total = samples + misses;
			double colorMult = exposure / samples;

			double r = color.R * colorMult;
			double g = color.G * colorMult;
			double b = color.B * colorMult;
			double a = 1;

			double backAlphaAmt = misses / total;
			double backAmt = backAlphaAmt * backA;

			r += (back.R - r) * backAmt;
			g += (back.G - g) * backAmt;
			b += (back.B - b) * backAmt;
			a += (backA - a) * backAlphaAmt;

			return GetColorCode(r,
				g,
				b,
				a);
		}

		/// <summary>
		/// Calculate the final color output for a pixel and convert to a .NET <see cref="Color"/>.
		/// </summary>
		/// <param name="back">The background color.</param>
		/// <param name="backA">The background alpha value.</param>
		/// <param name="exposure">The exposure used to brighten the image.</param>
		public Color GetColor(DoubleColor back, double backA, double exposure)
		{
			return Color.FromArgb(GetOutput(back, backA, exposure));
		}
	}
}
