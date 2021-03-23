using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Linq;

namespace RaytracerCore.Raytracing
{
	/// <summary>
	/// Manages the threading of the raytracer, and all the data that must be tracked between the threads.
	/// </summary>
	public class FullRaytracer
	{
		// Whether to randomize the sample order within each range a thread will work in.
		protected const bool Randomize = true;

		protected const int Interval = 100;

		public Scene Scene;
		public double Exposure;

		protected int Threads;

		protected SampleSet[,] SampleSets;
		protected readonly Action<FullRaytracer, string, double, Bitmap> UpdateStatusCallback;
		protected readonly Action<FullRaytracer, Bitmap> UpdateDebugCallback;

		protected volatile bool Stopping = false;
		protected volatile bool Running = false;
		protected volatile bool Paused = false;
		protected readonly List<Raytracer> RunningTracers = new List<Raytracer>();

		protected EventWaitHandle IntervalWaiter;
		protected EventWaitHandle MainWaiter;
		protected EventWaitHandle ChildWaiter;

		public readonly Raytracer DebugPathtracer;

		public readonly DebugRaycaster DebugRaycaster;
		protected bool DebugChanged = false;

		public FullRaytracer(Scene scene, int threads, Action<FullRaytracer, string, double, Bitmap> updateStatus, Action<FullRaytracer, Bitmap> updateDebug)
		{
			Scene = scene;
			Threads = threads;

			UpdateStatusCallback = updateStatus;
			UpdateDebugCallback = updateDebug;

			IntervalWaiter = new EventWaitHandle(true, EventResetMode.AutoReset);

			MainWaiter = new EventWaitHandle(!Paused, EventResetMode.ManualReset);
			ChildWaiter = new EventWaitHandle(!Paused, EventResetMode.ManualReset);

			DebugPathtracer = new Raytracer(this, scene, new PixelPos[0]);

			DebugRaycaster = new DebugRaycaster(scene);
		}
		
		/// <summary>
		/// Send a status update to the callback.
		/// </summary>
		/// <param name="status">The status string to display.</param>
		/// <param name="progress">The current progress value.</param>
		protected void UpdateStatus(string status, double progress)
		{
			UpdateStatusCallback?.Invoke(this, status, progress, GetBitmap());
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
		public static PixelPos[] GetPixelRange(Rectangle rect)
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
			SampleSet result = default;

			if (SampleSets != null)
			{
				x = Util.Clamp(x, 0, Scene.Width);
				y = Util.Clamp(y, 0, Scene.Height);
				result = SampleSets[x, y];
			}

			if (result == default)
				return new SampleSet();

			return result;
		}

		/// <summary>
		/// Get the SampleSet data at the specified pixel position.
		/// </summary>
		/// <param name="x">The pixel position.</param>
		public SampleSet GetSampleSet(PixelPos position)
		{
			return GetSampleSet(position.X, position.Y);
		}

		/// <summary>
		/// Add a sample to the specified pixel.
		/// </summary>
		/// <param name="pos">The pixel position to add the sample to.</param>
		/// <param name="color">The color of the sample.</param>
		public void AddSample(PixelPos pos, DoubleColor color)
		{
			SampleSets[pos.X, pos.Y].AddSample(color);
		}

		/// <summary>
		/// Add a miss to the specified pixel.
		/// </summary>
		/// <param name="pos">The pixel position to add a miss to.</param>
		public void AddMiss(PixelPos pos)
		{
			SampleSets[pos.X, pos.Y].AddMiss();
		}

		/// <summary>
		/// Convert the sample data to an output image.
		/// </summary>
		public unsafe Bitmap GetBitmap()
		{
			/*Stopwatch profile = new Stopwatch();
			profile.Start();*/

			if (SampleSets == null)
				return null;

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

		private void UpdateDebug()
		{
			if (DebugChanged)
			{
				DebugChanged = false;
				UpdateDebugCallback?.Invoke(this, DebugRaycaster.RenderDebug());
			}
		}

		/// <summary>
		/// Start the raytracer.
		/// </summary>
		public void Start()
		{
			while (Running) ;
			RunningTracers.Clear();
			Stopping = false;
			Running = true;

			UpdateStatus("Preparing scene...", 0);
			Scene.Prepare();

			int w = Scene.Width;
			int h = Scene.Height;

			// Initialize image storage
			SampleSets = new SampleSet[w, h];
			for (int x = 0; x < w; x++)
			{
				for (int y = 0; y < h; y++)
				{
					SampleSets[x, y] = new SampleSet();
				}
			}

			// Prepare camera
			Scene.Camera.InitRender(w, h);

			Random rand = new Random();

			UpdateStatus("Beginning render...", 0);

			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			// Initialize sets of pixels for threads to work with
			PixelPos[] allPixels = GetPixelRange(new Rectangle(0, 0, w, h));

			if (Randomize)
				allPixels = allPixels.OrderBy((p) => rand.NextDouble()).ToArray();

			for (int i = 0; i < Threads; i++)
			{
				int start = (int)((allPixels.Length / (double)Threads) * i);
				int end = (int)((allPixels.Length / (double)Threads) * (i + 1));
				Raytracer tracer = new Raytracer(this, Scene, allPixels[start..end]);
				RunningTracers.Add(tracer);
			}

			// Start threads
			foreach (Raytracer tracer in RunningTracers)
			{
				new Thread(tracer.Render).Start();
				while (!tracer.Running) ;
			}

			// Status/image updating loop
			Stopwatch sleepTimer = new Stopwatch();

			while (RunningTracers.Count > 0)
			{
				sleepTimer.Restart();

				if (Stopping)
				{
					foreach (Raytracer tracer in RunningTracers)
					{
						tracer.Stop = true;
						while (tracer.Running) ;
					}
				}

				bool finishedPausing = RunningTracers.All((t) => t.Paused);

				if (finishedPausing)
					stopwatch.Stop();

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

				UpdateDebug();

				//prevMs = estMs;

				// Wait for all tracers to pause and then pause this thread as well
				if (Paused && finishedPausing)
				{
					// Pause the stopwatch and check for pause, then resume once unblocked
					MainWaiter.WaitOne();

					if (!Paused)
						stopwatch.Start();
				}

				IntervalWaiter.WaitOne(Math.Max(0, Interval - (int)sleepTimer.ElapsedMilliseconds));
			}

			Running = false;
		}

		public bool IsRunning => Running;

		public void WaitForResume()
		{
			ChildWaiter.WaitOne();
		}

		public void Pause()
		{
			Paused = true;
			MainWaiter.Reset();
			ChildWaiter.Reset();
		}

		public void QueueUpdate()
		{
			// Skip the interval wait in the main thread to update ASAP.
			IntervalWaiter.Set();

			// If we're paused, unblock the main thread and then block it again when an update has finished.
			if (Paused)
			{
				MainWaiter.Set();
				MainWaiter.Reset();
			}
		}

		public void QueueDebugUpdate()
		{
			DebugChanged = true;
			QueueUpdate();
		}

		public bool IsPaused => Paused;

		public void Resume()
		{
			Paused = false;
			MainWaiter.Set();
			ChildWaiter.Set();
		}

		public void Stop()
		{
			Stopping = true;
			// Resume so threads can finish processing and exit.
			Resume();
		}

		public bool IsStopping => Stopping;
	}
}
