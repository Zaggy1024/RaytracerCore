using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Linq;
using System.Windows.Forms;

namespace RaytracerCore.Raytracing
{
	/// <summary>
	/// Manages the threading of the raytracer, and all the data that must be tracked between the threads.
	/// </summary>
	public class FullRaytracer
	{
		// Whether the raytracer threads will run on rectangle sections of the output frame.
		private const bool Sections = false;
		// Whether to randomize the sample order within each range a thread will work in.
		private const bool Randomize = true;

		// Whether to update the display instantly. (VERY slow)
		private const bool InstantUpdate = false;
		
		// The number of sections to use to split up threads.
		// When not in sectioned mode, the number of threads will still be H * V.
		// TODO: Make this automatic
		private const int PartsH = 4;
		private const int PartsV = 3;

		public Scene Scene;
		public double Exposure;

		private SampleSet[,] SampleSets;
		private readonly Action<FullRaytracer, string, double, Bitmap> UpdateStatusCallback;
		private readonly Action<FullRaytracer, PixelPos, Color> UpdatePixelCallback;

		public volatile bool Stop = false;
		public volatile bool Running = false;
		private readonly List<Raytracer> RunningTracers = new List<Raytracer>();

		public readonly Raytracer DebugRaytracer;

		public FullRaytracer(Scene scene, Action<FullRaytracer, string, double, Bitmap> updateStatus, Action<FullRaytracer, PixelPos, Color> updatePixel)
		{
			Scene = scene;
			UpdateStatusCallback = updateStatus;
			UpdatePixelCallback = updatePixel;

			DebugRaytracer = new Raytracer(this, scene, new PixelPos[0]);
		}

		/// <summary>
		/// Send a status update to the callback.
		/// </summary>
		/// <param name="status">The status string to display.</param>
		/// <param name="progress">The current progress value.</param>
		protected void UpdateStatus(string status, double progress)
		{
			if (InstantUpdate)
				UpdateStatusCallback?.Invoke(this, status, progress, null);
			else
				UpdateStatusCallback?.Invoke(this, status, progress, GetBitmap());
		}

		/// <summary>
		/// Send an update for an individual pixel to the callback.
		/// </summary>
		/// <param name="position">The pixel position to update.</param>
		private void UpdatePixel(PixelPos position)
		{
			if (InstantUpdate)
				UpdatePixelCallback?.Invoke(this, position, GetSampleSet(position).GetColor(Scene.BackgroundRGB, Scene.BackgroundAlpha, Exposure));
		}

		/// <summary>
		/// Format a seconds value to [mm]:ss.000
		/// </summary>
		/// <param name="seconds">The number of seconds to format.</param>
		protected static string FormatS(double seconds)
		{
			int mins = (int)(seconds / 60);
			double secs = seconds - mins * 60;

			return $"{mins:D}:{secs:00.000}";
		}

		/// <summary>
		/// Get all pixel positions in a rectangle.
		/// </summary>
		private PixelPos[] GetPixelRange(Rectangle rect)
		{
			PixelPos[] pixels = new PixelPos[rect.Width * rect.Height];

			for (int x = 0; x < rect.Width; x++)
			{
				for (int y = 0; y < rect.Height; y++)
				{
					pixels[x + y * rect.Width] = new PixelPos(rect.Left + x, rect.Top + y);
				}
			}

			return pixels;
		}

		/// <summary>
		/// Get the SampleSet data at the specified pixel position.
		/// </summary>
		/// <param name="x">The x pixel position.</param>
		/// <param name="y">The y pixel position.</param>
		public SampleSet GetSampleSet(int x, int y)
		{
			if (SampleSets == null)
				return default;

			return SampleSets[x, y];
		}

		/// <summary>
		/// Get the SampleSet data at the specified pixel position.
		/// </summary>
		/// <param name="x">The pixel position.</param>
		public SampleSet GetSampleSet(PixelPos position)
		{
			return GetSampleSet(position.x, position.y);
		}

		/// <summary>
		/// Add a sample to the specified pixel.
		/// </summary>
		/// <param name="pos">The pixel position to add the sample to.</param>
		/// <param name="color">The color of the sample.</param>
		public void AddSample(PixelPos pos, DoubleColor color)
		{
			SampleSets[pos.x, pos.y] = SampleSets[pos.x, pos.y].AddSample(color);
			UpdatePixel(pos);
		}

		/// <summary>
		/// Add a miss to the specified pixel.
		/// </summary>
		/// <param name="pos">The pixel position to add a miss to.</param>
		public void AddMiss(PixelPos pos)
		{
			SampleSets[pos.x, pos.y] = SampleSets[pos.x, pos.y].AddMiss();
			UpdatePixel(pos);
		}

		/// <summary>
		/// Convert the sample data to an output image.
		/// </summary>
		public unsafe Bitmap GetBitmap()
		{
			/*Stopwatch profile = new Stopwatch();
			profile.Start();*/

			Bitmap bitmap = new Bitmap(SampleSets.GetLength(0), SampleSets.GetLength(1));
			BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			int* ptr = (int*)data.Scan0.ToPointer();
			int bpp = Image.GetPixelFormatSize(data.PixelFormat) / 8;

			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					ptr[(y * data.Width) + x] = SampleSets[x, y].GetOutput(Scene.BackgroundRGB, Scene.BackgroundAlpha, Exposure);
				}
			}

			bitmap.UnlockBits(data);

			//Console.WriteLine($"Generating bitmap took {profile.Elapsed.TotalSeconds}");
			return bitmap;
		}

		/// <summary>
		/// Start the raytracer.
		/// </summary>
		public void Start()
		{
			while (Running) ;
			RunningTracers.Clear();
			Stop = false;
			Running = true;

			int w = Scene.Width;
			int h = Scene.Height;
			SampleSets = new SampleSet[w, h];

			Scene.Camera.InitRender(w, h);

			Random rand = new Random();

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			if (Sections)
			{
				double hPartSize = w / (double)PartsH;
				double vPartSize = h / (double)PartsV;

				for (int hPart = 0; (hPart < PartsH) && !Stop; hPart++)
				{
					int left = (int)(hPart * hPartSize);
					int right = (int)((hPart + 1) * hPartSize);

					for (int vPart = 0; (vPart < PartsV) && !Stop; vPart++)
					{
						int top = (int)(vPart * vPartSize);
						int bottom = (int)((vPart + 1) * vPartSize);

						PixelPos[] pixels = GetPixelRange(Rectangle.FromLTRB(left, top, right, bottom));

						if (Randomize)
							pixels = pixels.OrderBy((p) => rand.NextDouble()).ToArray();

						Raytracer tracer = new Raytracer(this, Scene, pixels);
						RunningTracers.Add(tracer);
					}
				}
			}
			else
			{
				PixelPos[] allPixels = GetPixelRange(new Rectangle(0, 0, w, h));

				if (Randomize)
					allPixels = allPixels.OrderBy((p) => rand.NextDouble()).ToArray();

				int parts = PartsH * PartsV;

				for (int i = 0; i < parts; i++)
				{
					int start = (int)((allPixels.Length / (double)parts) * i);
					int end = (int)((allPixels.Length / (double)parts) * (i + 1));
					Raytracer tracer = new Raytracer(this, Scene, allPixels.Skip(start).Take(end - start).ToArray());
					RunningTracers.Add(tracer);
				}
			}

			// Start threads
			int tracers = RunningTracers.Count;

			foreach (Raytracer tracer in RunningTracers)
			{
				new Thread(tracer.Render).Start();
				while (!tracer.Running) ;
			}

			Stopwatch sleepTimer = new Stopwatch();

			while (RunningTracers.Count > 0)
			{
				if (Stop)
				{
					foreach (Raytracer tracer in RunningTracers)
					{
						tracer.Stop = true;
						while (tracer.Running) ;
					}
				}
				else
				{
					foreach (Raytracer tracer in RunningTracers)
						tracer.Running = Running;
				}

				// Clean up stopped tracers
				RunningTracers.RemoveAll((t) => t.Stop && !t.Running);

				if (RunningTracers.Count <= 0)
					break;

				TimeSpan time = stopwatch.Elapsed;
				ulong samples = RunningTracers.Aggregate(0UL, (a, b) => a + b.Samples);
				double perPixel = samples / (double)(w * h);
				double progress = perPixel / (perPixel + 1000);

				UpdateStatus(
					string.Format("Elapsed: {0} Samples: {1:N0} {2:N3}/px/sec {3:N2}/px",
						Util.FormatTimeSpan(time),
						samples,
						(samples / time.TotalSeconds) / (w * h),
						perPixel),
					progress);

				//prevMs = estMs;

				sleepTimer.Start();
				Application.DoEvents();
				Thread.Sleep(Math.Max(0, 100 - (int)sleepTimer.ElapsedMilliseconds));
			}

			if (!Stop)
			{
				UpdateStatus("time: " + FormatS(stopwatch.Elapsed.TotalSeconds), 1);

				new Thread(() =>
						{
							/*if (scene.outputPath != null)
							{
								try
								{
									File file = new File(scene.outputPath);
									ImageIO.write(image, "png", file);
									System.out.println("Saved image to \"" + scene.outputPath + "\".");
								}
								catch (IOException e)
								{
									System.out.println("Error saving image to \"" + scene.outputPath + "\".");
									e.printStackTrace();
								}
							}*/
						}).Start();
			}

			Running = false;
		}
	}
}
