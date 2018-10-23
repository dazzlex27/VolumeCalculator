using Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FrameProviders;
using VolumeCalculatorGUI.Entities;
using VolumeCalculatorGUI.GUI.Utils;
using VolumeCalculatorGUI.Utils;

namespace VolumeCalculatorGUI.GUI
{
	internal class MainWindowVm : BaseViewModel
	{
		private event Action<ApplicationSettings> ApplicationSettingsChanged;

		private readonly ILogger _logger;

		private ApplicationSettings _settings;

		private StreamViewControlVm _streamViewControlVm;
		private CalculationDashboardControlVm _calculationDashboardControlVm;
		private TestDataGenerationControlVm _testDataGenerationControlVm;

		private readonly KeyboardListener _keyboardListener;

		public string WindowTitle => Constants.AppHeaderString;

		public StreamViewControlVm StreamViewControlVm
		{
			get => _streamViewControlVm;
			set
			{
				if (Equals(_streamViewControlVm, value))
					return;

				_streamViewControlVm = value;
				OnPropertyChanged();
			}
		}

		public CalculationDashboardControlVm CalculationDashboardControlVm
		{
			get => _calculationDashboardControlVm;
			set
			{
				if (ReferenceEquals(_calculationDashboardControlVm, value))
					return;

				_calculationDashboardControlVm = value;
				OnPropertyChanged();
			}
		}

		public TestDataGenerationControlVm TestDataGenerationControlVm
		{
			get => _testDataGenerationControlVm;
			set
			{
				if (ReferenceEquals(_testDataGenerationControlVm, value))
					return;

				_testDataGenerationControlVm = value;
				OnPropertyChanged();
			}
		}

		private ApplicationSettings Settings
		{
			get => _settings;
			set
			{
				if (_settings == value)
					return;

				_settings = value;
				ApplicationSettingsChanged?.Invoke(value);
			}
		}

		public ICommand OpenSettingsCommand { get; }

		public MainWindowVm()
		{
			try
			{
				_logger = new Logger();
				_logger.LogInfo("Starting up...");
				_logger.LogInfo($"App: {Constants.AppHeaderString}");
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

				OpenSettingsCommand = new CommandHandler(OpenSettings, true);

				InitializeSettings();
				InitializeSubViewModels();

				_keyboardListener = new KeyboardListener();
				_keyboardListener.CharSequenceFormed += KeyboardListener_CharSequenceFormed;

				_logger.LogInfo("Application is initalized");
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to initialize the application!", ex);
				MessageBox.Show("Ошибка инициализации приложения, информация записана в журнал.", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
				Process.GetCurrentProcess().Kill();
			}
		}

		private void KeyboardListener_CharSequenceFormed(string charSequence)
		{
			_calculationDashboardControlVm?.UpdateCodeText(charSequence);
		}

		public void ExitApplication()
		{
			try
			{
				_logger.LogInfo("Shutting down...");

				_keyboardListener.CharSequenceFormed -= KeyboardListener_CharSequenceFormed;

				_logger.LogInfo("Saving settings...");
				IoUtils.SerializeSettings(_settings);

				_logger.LogInfo("Disposing libs...");
				_streamViewControlVm.Dispose();
				_calculationDashboardControlVm.Dispose();

				_logger.LogInfo("App terminated");
				Application.Current.Shutdown();
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to complete application clean up", ex);
			}
		}

		public void OpenSettings()
		{
			try
			{
				var deviceParams = _streamViewControlVm.GetDepthCameraParams();
				var depthMapProcessor = CalculationDashboardControlVm.GetDepthMapProcessor();

				var settingsWindowVm = new SettingsWindowVm(_logger, _settings, deviceParams, depthMapProcessor);
				_streamViewControlVm.RawDepthFrameReady += settingsWindowVm.DepthFrameUpdated;
				var settingsWindow = new SettingsWindow
				{
					Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
					DataContext = settingsWindowVm
				};

				var settingsChanged = settingsWindow.ShowDialog() == true;
				_streamViewControlVm.RawDepthFrameReady -= settingsWindowVm.DepthFrameUpdated;

				if (!settingsChanged)
					return;

				Settings = settingsWindowVm.GetSettings();
				IoUtils.SerializeSettings(Settings);
				_logger.LogInfo("New settings have been applied: " +
								$"floorDepth={_settings.DistanceToFloor} useAreaMask={_settings.UseAreaMask} minObjHeight={_settings.MinObjHeight} sampleCount={_settings.SampleCount} outputPath={_settings.OutputPath}");
			}
			catch (Exception ex)
			{
				_logger.LogException("Exception occured during a settings change", ex);
				MessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public bool IsTestDataGenerationControlVisible
		{
			get => _testDataGenerationControlVm?.ShowControl ?? false;
			set
			{
				if (_testDataGenerationControlVm == null)
					return;

				if (_testDataGenerationControlVm.ShowControl == value)
					return;

				_testDataGenerationControlVm.ShowControl = value;
				OnPropertyChanged();
			}
		}

		private void InitializeSubViewModels()
		{
			_streamViewControlVm = new StreamViewControlVm(_logger, _settings);

			var colorCameraParams = _streamViewControlVm.GetColorCameraParams();
			var depthCameraParams = _streamViewControlVm.GetDepthCameraParams();

			_calculationDashboardControlVm = new CalculationDashboardControlVm(_logger, _settings, colorCameraParams, depthCameraParams);
			_testDataGenerationControlVm = new TestDataGenerationControlVm(_settings, depthCameraParams);

			ApplicationSettingsChanged += OnApplicationSettingsChanged;
			_streamViewControlVm.DeviceParamsChanged += OnDeviceParametersChanged;
			_streamViewControlVm.ColorFrameReady += OnColorFrameReady;
			_streamViewControlVm.DepthFrameReady += OnDepthFrameReady;
		}

		private void OnDeviceParametersChanged(ColorCameraParams arg1, DepthCameraParams arg2)
		{
			_calculationDashboardControlVm.DeviceParamsUpdated(arg1, arg2);
			_testDataGenerationControlVm.DeviceParamsUpdated(arg1, arg2);
		}

		private void OnApplicationSettingsChanged(ApplicationSettings settings)
		{
			_calculationDashboardControlVm.ApplicationSettingsUpdated(settings);
			_streamViewControlVm.ApplicationSettingsUpdated(settings);
			_testDataGenerationControlVm.ApplicationSettingsUpdated(settings);
		}

		private void OnColorFrameReady(ImageData image)
		{
			_calculationDashboardControlVm.ColorFrameArrived(image);
			_testDataGenerationControlVm.ColorFrameUpdated(image);
		}

		private void OnDepthFrameReady(DepthMap depthMap)
		{
			_calculationDashboardControlVm.DepthFrameArrived(depthMap);
			_testDataGenerationControlVm.DepthFrameUpdated(depthMap);
		}

		private void InitializeSettings()
		{
			_logger.LogInfo("Trying to read settings from file...");
			var settingsFromFile = IoUtils.DeserializeSettings();
			if (settingsFromFile == null)
			{
				_logger.LogInfo("Failed to read settings from file, will use default settings");
				Settings = ApplicationSettings.GetDefaultSettings();
			}
			else
				Settings = settingsFromFile;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);
			MessageBox.Show("Произошла критическая ошибка, приложение будет закрыто. Информация записана в журнал",
				"Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}