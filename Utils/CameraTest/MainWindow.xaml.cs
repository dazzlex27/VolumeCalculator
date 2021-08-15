using Primitives.Settings;
using ProcessingUtils;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CameraTest
{
	public partial class MainWindow : INotifyPropertyChanged
	{
		private WriteableBitmap _colorImageBitmap;
		private readonly Proline2520Controller _controller;

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public WriteableBitmap ColorImageBitmap
		{
			get => _colorImageBitmap;
			set
			{
				if (_colorImageBitmap == value)
					return;

				_colorImageBitmap = value;
				OnPropertyChanged();
			}
		}

		public MainWindow()
		{
			InitializeComponent();

			var httpClient = new HttpClient();
			_controller = new Proline2520Controller(httpClient);

			var settings = ReadSettingsFromFile();
			var ipCameraSettings = settings.IoSettings.IpCameraSettings;
			IpBox.Text = ipCameraSettings.Ip;
			LoginBox.Text = ipCameraSettings.Login;
			PasswordBox.Text = ipCameraSettings.Password;
			PresetBox.Text = (ipCameraSettings.ActivePreset + 1).ToString();
		}

		private static ApplicationSettings ReadSettingsFromFile()
		{
			ApplicationSettings settings;

			try
			{
				var settingsFromFile = IoUtils.DeserializeSettings<ApplicationSettings>();
				if (settingsFromFile != null)
					return settingsFromFile;
			}
			finally
			{
				settings = ApplicationSettings.GetDefaultSettings();
			}

			return settings;
		}

		private async void OnStopClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.StopAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to stop: " + ex);
			}
		}

		private async void OnLeftClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.PanLeftAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to pan left: " + ex);
			}
		}

		private async void OnUpClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.TiltUpAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to tilt up: " + ex);
			}
		}

		private async void OnDownClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.TiltDownAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to tilt down: " + ex);
			}
		}

		private async void OnRightClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.PanRightAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to pan right: " + ex);
			}
		}

		private async void OnConnectClicked(object sender, RoutedEventArgs e)
		{
			var initialized = true;

			try
			{
				var ip = IpBox.Text;
				var login = LoginBox.Text;
				var password = PasswordBox.Text;
				var status = await _controller.InitializeAsync(ip, login, password);
				MessageBox.Show(status.ToString());
			}
			catch(Exception ex)
			{
				initialized = false;
				MessageBox.Show("Failed to connect to device: " + ex);
			}

			if (initialized)
				await Task.Run(async () => { await RunSnapshotFetchingLoop(); });
		}

		private async Task RunSnapshotFetchingLoop()
		{
			try
			{
				while (true)
				{
					var imageData = await _controller.GetSnapshotAsync();
					Dispatcher.Invoke(() =>
					{
						ColorImageBitmap = GraphicsUtils.GetWriteableBitmapFromImageData(imageData);
					});
					await Task.Delay(40);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show("Failed to pull snapshot from the camera: " + ex);
			}
		}

		private async void OnGoToPreset1Clicked(object sender, RoutedEventArgs e)
		{
			try
			{
				var index = int.Parse(PresetBox.Text);
				var presetIndex = index - 1;
				var presetOk = await _controller.GoToPresetAsync(presetIndex);
				if (!presetOk)
					MessageBox.Show($"Preset {index} was not found");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to go to preset {PresetBox.Text}: " + ex);
			}
		}

		private async void OnSetPresetClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				var index = int.Parse(PresetBox.Text);
				var presetIndex = index - 1;
				var presetOk = await _controller.SetPresetAsync(presetIndex);
				if (!presetOk)
					MessageBox.Show($"Preset {index} was not found");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to set preset {PresetBox.Text}: " + ex);
			}
		}

		private async void OnZoomInClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.ZoomInAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to zoom in: " + ex);
			}
		}

		private async void OnZoomOutClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await _controller.ZoomOutAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to zoom out: " + ex);
			}
		}
	}
}