using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;

using RaytracerCore.Raytracing;
using RaytracerCore.Inspector;
using System.Runtime.CompilerServices;

namespace RaytracerCore
{
	public partial class MainWindow : Form
	{
		readonly SynchronizationContext Context;

		FullRaytracer CurrentRaytracer;
		string CurrentPath;

		readonly List<RayInspector> OpenInspectors = new List<RayInspector>();
		SceneInspector SceneInspector;

		Image RenderedImage = null;
		Image DebugImage = null;
		bool DisplayDebug = false;
		object DebugItem = null;

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public MainWindow()
		{
			InitializeComponent();

			Context = SynchronizationContext.Current;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			numericExposure.Value = 1.0M;
			UpdateBackgroundColor();
			UpdateSceneButtons();
		}

		private void UpdateImages()
		{
			Util.Assert(SynchronizationContext.Current == Context, "Do not update the displayed image off the GUI thread.");
			Util.Assert(DebugImage == null || RenderedImage.Size == DebugImage.Size, "Rendered and debug image sizes do not match.");

			if (imageRendered.Image == null || imageRendered.Image.Size != RenderedImage.Size)
			{
				imageRendered.Image = new Bitmap(RenderedImage.Width, RenderedImage.Height);
				imageRendered.Size = RenderedImage.Size;
			}

			using (Graphics graphics = Graphics.FromImage(imageRendered.Image))
			{
				graphics.Clear(Color.Transparent);

				if (RenderedImage != null)
					graphics.DrawImage(RenderedImage, Point.Empty);

				if (DebugImage != null && DisplayDebug)
				{
					ImageAttributes attributes = new ImageAttributes();
					attributes.SetColorMatrix(new ColorMatrix() { Matrix33 = .5F });
					graphics.DrawImage(DebugImage,
						new Rectangle(Point.Empty, DebugImage.Size),
						0, 0, DebugImage.Width, DebugImage.Height,
						GraphicsUnit.Pixel, attributes);
				}
			}

			imageRendered.Invalidate();
		}

		private void RestartRender(Action change)
		{
			new Thread(() =>
			{
				if (CurrentRaytracer != null)
				{
					// If we're already stopping, another thread is either restarting or changing scenes.
					// Don't start a new render thread on the same raytracer.
					if (CurrentRaytracer.IsStopping)
					{
						change?.Invoke();
						return;
					}

					CurrentRaytracer.Stop();

					while (CurrentRaytracer.IsRunning) ;
				}

				change?.Invoke();

				if (CurrentRaytracer != null)
				{
					Context.Post((s) =>
					{
						Bitmap image = new Bitmap(CurrentRaytracer.Scene.Width, CurrentRaytracer.Scene.Height, PixelFormat.Format32bppArgb);
						Graphics.FromImage(image).Clear(CurrentRaytracer.Scene.BackgroundRGB.ToColor(CurrentRaytracer.Scene.BackgroundAlpha));
						RenderedImage = image;
						UpdateImages();

						UpdateSceneButtons();

						SceneInspector?.ChangeScene(CurrentRaytracer.Scene);
					}, null);

					CurrentRaytracer.Start();
				}
			}).Start();
		}

		private void RestartRender()
		{
			RestartRender(null);
		}

		private void UpdateRenderedImage(FullRaytracer raytracer, string statusText, double progress, Bitmap bitmap)
		{
			Context.Post((v) => {
				if (raytracer == CurrentRaytracer && Visible)
				{
					labelStatus.Text = statusText;

					barProgress.Value = Math.Min((int)Math.Round(progress * 100, 2), 100);
					labelProgress.Text = $"{progress * 100:F1}%";
					statusStrip.Update();

					if (bitmap != null)
					{
						RenderedImage = bitmap;
						UpdateImages();
					}
				}
			}, null);
		}

		private void UpdateDebugImage(FullRaytracer raytracer, Bitmap bitmap)
		{
			Context.Post((v) => {
				if (raytracer == CurrentRaytracer && Visible)
				{
					if (bitmap != null)
					{
						DebugImage = bitmap;
						UpdateImages();
					}
				}
			}, null);
		}

		private void LoadScene(string path)
		{
			Scene scene = null;

#if !DEBUG
			try
#endif
			{
				scene = SceneLoader.FromFile(path);
			}
#if !DEBUG
			catch (LoaderException e)
			{
				MessageBox.Show(this, $"{e.Message}{(e.InnerException != null ? $": {e.InnerException.Message}" : "")}", "Error while loading scene");
			}
#endif

			if (scene != null)
			{
				buttonScene.Text = Path.GetFileName(path);

				comboCamera.Items.Clear();
				for (int i = 0; i < scene.Cameras.Count; i++)
					comboCamera.Items.Add($"Camera {i}");
				comboCamera.SelectedIndex = 0;

				CloseInspectors();

				RestartRender(() => {
					CurrentRaytracer = new FullRaytracer(scene, Environment.ProcessorCount, UpdateRenderedImage, UpdateDebugImage);
					SetExposure();

					CurrentPath = path;
				});
			}
		}

		private void UpdateCurrentImage()
		{
			if (CurrentRaytracer != null)
			{
				CurrentRaytracer.QueueUpdate();
			}
		}

		private void openSceneMenuItem_Click(object sender, EventArgs e)
		{
			using OpenFileDialog dialog = new OpenFileDialog();
			dialog.InitialDirectory = Application.StartupPath;
			dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
			dialog.RestoreDirectory = false;

			if (dialog.ShowDialog(this) == DialogResult.OK)
			{
				LoadScene(dialog.FileName);
			}
		}

		private void saveOutputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using SaveFileDialog dialog = new SaveFileDialog();
			dialog.InitialDirectory = Application.StartupPath;
			dialog.Filter = "Portable Network Graphics|*.png|JPEG|*.jpg|Bitmap|*.bmp";
			dialog.RestoreDirectory = false;

			if (dialog.ShowDialog(this) == DialogResult.OK)
			{
				ImageFormat format;

				switch (Path.GetExtension(dialog.FileName))
				{
					case ".jpg":
					case ".jpeg":
						format = ImageFormat.Jpeg;
						break;
					case ".bmp":
						format = ImageFormat.Bmp;
						break;
					case ".png":
					default:
						format = ImageFormat.Png;
						break;
				}

				RenderedImage.Save(dialog.FileName, format);
			}
		}

		private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (CurrentRaytracer != null)
				CurrentRaytracer.Stop();
		}

		private void comboCamera_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (CurrentRaytracer != null)
			{
				CurrentRaytracer.Scene.CurrentCamera = Math.Min(comboCamera.SelectedIndex, CurrentRaytracer.Scene.Cameras.Count);
				RestartRender();
			}
		}

		private void SetExposure()
		{
			if (CurrentRaytracer != null)
				CurrentRaytracer.Exposure = (double)numericExposure.Value;
		}

		private void sliderExposure_Scroll(object sender, EventArgs e)
		{
			numericExposure.Value = sliderExposure.Value / 100M;
		}

		private void numericExposure_ValueChanged(object sender, EventArgs e)
		{
			sliderExposure.Value = Math.Min((int)(numericExposure.Value * 100M), sliderExposure.Maximum);
			SetExposure();

			UpdateCurrentImage();
		}

		private void UpdateBackgroundColor()
		{
			if (int.TryParse(textBackground.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int result))
			{
				panelPreview.BackColor = Color.FromArgb(result);
			}
		}

		private void textBackground_TextChanged(object sender, EventArgs e)
		{
			UpdateBackgroundColor();
		}

		private void UpdateSceneButtons()
		{
			bool hasScene = CurrentRaytracer != null;
			buttonScene.Visible = hasScene;
			buttonReload.Enabled = hasScene;
			buttonPause.Enabled = hasScene;

			if (CurrentRaytracer?.IsPaused == false)
				buttonPause.Text = "❚❚";
			else
				buttonPause.Text = "▶";
		}

		private void buttonReload_Click(object sender, EventArgs e)
		{
			if (CurrentPath != null)
				LoadScene(CurrentPath);
		}

		private void buttonPause_Click(object sender, EventArgs e)
		{
			if (CurrentRaytracer != null)
			{
				if (CurrentRaytracer.IsPaused)
					CurrentRaytracer.Resume();
				else
					CurrentRaytracer.Pause();
			}

			UpdateSceneButtons();
		}

		public void CloseInspectors()
		{
			while (OpenInspectors.Count > 0)
				OpenInspectors[0].Close();
		}

		private void renderedImageBox_Click(object sender, MouseEventArgs mouseEvent)
		{
			if (CurrentRaytracer != null && mouseEvent.Button == MouseButtons.Left)
			{
				RayInspector inspector = new RayInspector(CurrentRaytracer);
				inspector.SetPosition(mouseEvent.X, mouseEvent.Y);
				inspector.Show(this);
				inspector.FormClosed += (o, e) =>
					OpenInspectors.Remove(inspector);
				OpenInspectors.Add(inspector);
			}
		}

		private void RefreshDebugOverlay()
		{
			DebugImage = null;
			CurrentRaytracer.QueueDebugUpdate();
		}

		private void renderedImageBox_MouseMove(object sender, MouseEventArgs mouseEvent)
		{
			if (CurrentRaytracer != null)
			{
				SampleSet samples = CurrentRaytracer.GetSampleSet(mouseEvent.X, mouseEvent.Y);
				labelSample.Text = $"Color: {samples.Color} " +
					$"Total: {samples.Samples + samples.Misses} " +
					$"Missed: {samples.Misses} " +
					$"Color: {samples.GetColor(CurrentRaytracer.Scene.BackgroundRGB, CurrentRaytracer.Scene.BackgroundAlpha, CurrentRaytracer.Exposure).ToArgb():X8}";
			}
		}

		private void SetDebugMode(SceneInspector.DisplayCategory category)
		{
			switch (category)
			{
				case SceneInspector.DisplayCategory.Scene:
					CurrentRaytracer.DebugRaycaster.SetMode(DebugRaycaster.DisplayMode.Primitives);
					break;
				case SceneInspector.DisplayCategory.BVH:
					CurrentRaytracer.DebugRaycaster.SetMode(DebugRaycaster.DisplayMode.BoundingVolumes);
					break;
			}
		}

		private void SceneInspector_DisplaySettingChanged(object sender, SceneInspector.DisplaySettingChangedEventArgs e)
		{
			if (CurrentRaytracer != null && sender is SceneInspector inspector && inspector == SceneInspector)
			{
				bool refresh = false;

				if (e.ChangedSettings.HasFlag(SceneInspector.DisplaySettingField.OverlayEnabled))
				{
					DisplayDebug = e.Settings.OverlayEnabled;
					refresh = DisplayDebug && DebugImage == null;
				}

				if (e.ChangedSettings.HasFlag(SceneInspector.DisplaySettingField.Category))
				{
					SetDebugMode(e.Settings.Category);
					refresh = true;
				}

				if (e.ChangedSettings.HasFlag(SceneInspector.DisplaySettingField.DisplaySelected)
					|| e.ChangedSettings.HasFlag(SceneInspector.DisplaySettingField.Selection))
				{
					object newDebugItem = null;

					if (e.Settings.DisplaySelected)
					{
						if (CurrentRaytracer.DebugRaycaster.SetDisplayOnly(e.Settings.Selection))
							newDebugItem = e.Settings.Selection;
					}

					// If our effective display item has changed, update the overlay if needed
					if (newDebugItem != DebugItem)
					{
						if (newDebugItem == null)
						{
							CurrentRaytracer.DebugRaycaster.ClearDisplayOnly();
							SetDebugMode(e.Settings.Category);
						}

						DebugItem = newDebugItem;
						refresh = true;
					}
				}

				if (refresh)
				{
					// Refresh the overlay now, or set it to null to be refreshed on next display if disabled
					if (DisplayDebug)
						RefreshDebugOverlay();
					else
						DebugImage = null;
				}
			}
		}

		private void SceneInspector_Disposed(object sender, EventArgs e)
		{
			SceneInspector = null;
		}

		private void OpenSceneInspector()
		{
			if (CurrentRaytracer != null)
			{
				if (SceneInspector == null)
				{
					SceneInspector = new SceneInspector(CurrentRaytracer.Scene);
					SceneInspector.DisplaySettingChanged += SceneInspector_DisplaySettingChanged;
					SceneInspector.Disposed += SceneInspector_Disposed;
				}

				if (SceneInspector.Visible)
					SceneInspector.Select();
				else
					SceneInspector.Show(this);
			}
		}

		private void buttonScene_Click(object sender, EventArgs e)
		{
			OpenSceneInspector();
		}
	}
}
