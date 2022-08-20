using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Linq;

using RaytracerCore;

namespace RaytracerCore.Raytracing
{
	/// <summary>
	/// Manages the threading of the raytracer, and all the data that must be tracked between the threads.
	/// </summary>
	public class FullRaytracer
	{
		protected struct ImageUpdate
		{
			public Rectangle Tile;
			public DoubleColor[,] Samples;
			public TimeSpan Elapsed;

			public ImageUpdate(Rectangle tile, DoubleColor[,] samples, TimeSpan elapsed)
			{
				Tile = tile;
				Samples = samples;
				Elapsed = elapsed;
			}
		}

		protected const int Interval = 100;

		public Scene Scene;
		public double Exposure;

		protected int Threads;
		protected int TilesX;
		protected int TilesY;
		protected Rectangle[] Tiles;

		protected SampleSet[,] SampleSets;
		protected readonly Action<FullRaytracer, string, double, Bitmap> UpdateStatusCallback;
		protected readonly Action<FullRaytracer, Bitmap> UpdateDebugCallback;

		protected volatile bool Stopping = false;
		protected volatile bool Running = false;
		protected volatile bool Paused = false;
		protected readonly List<Raytracer> RunningTracers = new List<Raytracer>();

		protected readonly object NextTileLock = new object();
		protected volatile int NextTile;
		protected readonly ConcurrentQueue<ImageUpdate> ImageUpdates = new ConcurrentQueue<ImageUpdate>();

		// Pauses on the update loop to free CPU time.
		protected EventWaitHandle IntervalWaiter;
		// Pauses until the UI thread unpauses the rendering.
		protected EventWaitHandle PauseWaiter;

		public readonly Raytracer DebugPathtracer;

		public readonly DebugRaycaster DebugRaycaster;
		protected bool DebugChanged = false;

		public FullRaytracer(Scene scene, int threads, Action<FullRaytracer, string, double, Bitmap> updateStatus, Action<FullRaytracer, Bitmap> updateDebug)
		{
			Scene = scene;

			Threads = threads;
			TilesY = (int)Math.Floor(Math.Sqrt(threads));
			TilesX = threads / TilesY;

			UpdateStatusCallback = updateStatus;
			UpdateDebugCallback = updateDebug;

			IntervalWaiter = new EventWaitHandle(true, EventResetMode.AutoReset);

			PauseWaiter = new EventWaitHandle(true, EventResetMode.ManualReset);

			DebugPathtracer = new Raytracer(this, scene);

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

		/// <summary>
		/// Add a finished rectangle to the end of the queue.
		/// </summary>
		public void OnTileFinished(Rectangle tile, DoubleColor[,] samples, TimeSpan elapsed)
		{
			ImageUpdates.Enqueue(new ImageUpdate(tile, samples, elapsed));
			QueueUpdate();
		}

		/// <summary>
		/// Request a new working tile. Will pause the thread until work is available.
		/// </summary>
		public Rectangle GetWorkingTile()
		{
			lock (NextTileLock)
			{
				while (NextTile == 0 && Paused)
					PauseWaiter.WaitOne();
				Rectangle tile = Tiles[NextTile];
				NextTile = (NextTile + 1) % Tiles.Length;
				return tile;
			}
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
			ImageUpdates.Clear();
			Stopping = false;
			Running = true;
			NextTile = 0;

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

			Tiles = new Rectangle[TilesX * TilesY];

			// Push tiles to the worker threads
			for (int x = 0; x < TilesX; x++)
			{
				int left = x * w / TilesX;
				int right = (x + 1) * w / TilesX;

				for (int y = 0; y < TilesY; y++)
				{
					int top = y * h / TilesY;
					int bottom = (y + 1) * h / TilesY;

					Tiles[y * TilesX + x] = Rectangle.FromLTRB(left, top, right, bottom);
				}
			}

			Random rand = new Random();

			UpdateStatus("Beginning render...", 0);

			TimeSpan totalTime = TimeSpan.Zero;
			uint totalTiles = 0;
			ulong totalSamples = 0;

			// Create and start worker threads
			for (int i = 0; i < Threads; i++)
			{
				Raytracer tracer = new Raytracer(this, Scene);
				RunningTracers.Add(tracer);
				new Thread(tracer.Render) { Priority = ThreadPriority.BelowNormal }.Start();
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

				// Clean up stopped tracers
				RunningTracers.RemoveAll((t) => t.Stop && !t.Running);

				if (RunningTracers.Count <= 0)
					break;

				while (ImageUpdates.TryDequeue(out ImageUpdate update))
				{
					for (int x = 0; x < update.Tile.Width; x++)
					{
						for (int y = 0; y < update.Tile.Height; y++)
						{
							DoubleColor color = update.Samples[x, y];
							PixelPos pixel = new PixelPos(update.Tile.Left + x, update.Tile.Top + y);
							if (color == DoubleColor.Placeholder)
								AddMiss(pixel);
							else
								AddSample(pixel, color);
						}
					}

					totalTime += update.Elapsed;
					totalTiles++;
					totalSamples += (ulong)(update.Tile.Width * update.Tile.Height);
				}

				TimeSpan averageTime = TimeSpan.Zero;
				if (totalTiles != 0)
					averageTime = totalTime / Threads;

				double perPixel = totalSamples / (double)(w * h);
				double samplesPerSecond = perPixel / averageTime.TotalSeconds;
				// 1000 samples per pixel = 50% progress, but progress never reaches 100%
				double progress = perPixel / (perPixel + 1000);

				UpdateStatus($"Tiles: {totalTiles:N0} Elapsed: {Util.FormatTimeSpan(averageTime)} " +
					$"{perPixel:N2}/px {samplesPerSecond:N3}/px/sec",
					progress);

				UpdateDebug();

				// Wait if we are paused.
				if (Paused)
				{
					PauseWaiter.WaitOne();
					if (Paused)
						PauseWaiter.Reset();
				}

				IntervalWaiter.WaitOne(Math.Max(0, Interval - (int)sleepTimer.ElapsedMilliseconds));
			}

			Running = false;
		}

		public bool IsRunning => Running;

		public void Pause()
		{
			Paused = true;
			PauseWaiter.Reset();
		}

		public void QueueUpdate()
		{
			// Skip the interval wait in the main thread to update ASAP.
			IntervalWaiter.Set();

			// If we're paused, unblock the main thread, it will be blocked again once a loop completes.
			if (Paused)
			{
				PauseWaiter.Set();
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
			PauseWaiter.Set();
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
