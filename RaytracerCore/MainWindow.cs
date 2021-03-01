using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Globalization;

using RaytracerCore.Raytracing;
using RaytracerCore.Vectors;
using RaytracerCore.Inspector;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

namespace RaytracerCore
{
	public partial class MainWindow : Form
	{
		readonly SynchronizationContext Context;

		FullRaytracer CurrentRaytracer;
		string CurrentPath;

		readonly List<RayInspector> OpenInspectors = new List<RayInspector>();

		[MethodImpl(MethodImplOptions.AggressiveOptimization)]
		public MainWindow()
		{
			InitializeComponent();

			Context = SynchronizationContext.Current;
			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			numericExposure.Value = 1.0M;
			UpdateBackgroundColor();
			UpdatePauseButton();
		}

		private void UpdateImage(Bitmap bitmap)
		{
			Util.Assert(SynchronizationContext.Current == Context, "Do not update the displayed image off the GUI thread.");

			if (bitmap == null)
				return;

			renderedImageBox.Image = bitmap;
			renderedImageBox.Width = bitmap.Width;
			renderedImageBox.Height = bitmap.Height;
			renderedImageBox.Refresh();
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
						UpdateImage(image);

						UpdatePauseButton();
					}, null);

					CurrentRaytracer.Start();

				}
			}).Start();
		}

		private void RestartRender()
		{
			RestartRender(null);
		}

		private void UpdateStatus(FullRaytracer raytracer, string statusText, double progress, Bitmap bitmap)
		{
			Context.Post((v) => {
				if (raytracer == CurrentRaytracer && Visible)
				{
					labelStatus.Text = statusText;

					barProgress.Value = Math.Min((int)Math.Round(progress * 100, 2), 100);
					labelProgress.Text = $"{progress * 100:F1}%";
					statusStrip.Update();

					if (bitmap != null)
						UpdateImage(bitmap);
				}
			}, null);
		}

		private void SetPixel(FullRaytracer raytracer, PixelPos pos, Color color)
		{
			Context.Post((v) => {
				if (raytracer == CurrentRaytracer && renderedImageBox.Image is Bitmap bitmap)
				{
					bitmap.SetPixel(pos.x, pos.y, color);
					renderedImageBox.Invalidate();
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
				labelScene.Text = Path.GetFileName(path);

				comboCamera.Items.Clear();
				for (int i = 0; i < scene.Cameras.Count; i++)
					comboCamera.Items.Add($"Camera {i}");
				comboCamera.SelectedIndex = 0;

				CloseInspectors();

				RestartRender(() => {
					CurrentRaytracer = new FullRaytracer(scene, UpdateStatus, SetPixel);
					CurrentPath = path;
					SetExposure();
				});
			}
		}

		private void UpdateCurrentImage()
		{
			if (CurrentRaytracer != null)
			{
				// If we're in the GUI thread, update asynchronously using thread pool.
				/*if (SynchronizationContext.Current == Context)
				{
					ThreadPool.QueueUserWorkItem((o) => UpdateCurrentImage());
					return;
				}

				UpdateImage(CurrentRaytracer.GetBitmap());*/
				CurrentRaytracer.QueueUpdate();
			}
		}

		private void openSceneMenuItem_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dialog = new OpenFileDialog())
			{
				dialog.InitialDirectory = Application.StartupPath;
				dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
				dialog.RestoreDirectory = false;

				if (dialog.ShowDialog(this) == DialogResult.OK)
				{
					LoadScene(dialog.FileName);
				}
			}

			//LoadScene("..\\..\\Debug\\bounce.txt");
		}

		private void saveOutputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
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

					renderedImageBox.Image.Save(dialog.FileName, format);
				}
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

			//if (CurrentRaytracer?.IsPaused() == true)
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

		private void buttonReload_Click(object sender, EventArgs e)
		{
			if (CurrentPath != null)
				LoadScene(CurrentPath);
		}

		private void UpdatePauseButton()
		{
			if (CurrentRaytracer == null)
			{
				buttonPause.Enabled = false;
				buttonPause.Text = "▶";
			}
			else
			{
				buttonPause.Enabled = true;

				if (CurrentRaytracer.IsPaused)
					buttonPause.Text = "▶";
				else
					buttonPause.Text = "❚❚";
			}
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

			UpdatePauseButton();
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

		private void checkDebug_CheckedChanged(object sender, EventArgs e)
		{
			if (CurrentRaytracer != null)
			{
				//CurrentRaytracer.Running = !checkDebug.Checked;
				CurrentRaytracer.Scene.DebugGeom = checkDebug.Checked;
				RestartRender();
			}
		}

		private void renderedImageBox_MouseMove(object sender, MouseEventArgs mouseEvent)
		{
			if (CurrentRaytracer != null)
			{
				SampleSet samples = CurrentRaytracer.GetSampleSet(mouseEvent.X, mouseEvent.Y);
				labelSample.Text = $"Color: {samples.color} " +
					$"Total: {samples.samples + samples.misses} " +
					$"Missed: {samples.misses} " +
					$"Color: {samples.GetColor(CurrentRaytracer.Scene.BackgroundRGB, CurrentRaytracer.Scene.BackgroundAlpha, CurrentRaytracer.Exposure).ToArgb():X8}";
			}
		}
	}
}
