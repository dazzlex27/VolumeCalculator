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

		private KeyboardListener _keyboardListener;
		private SerialPortListener _serialListener;

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

		public ICommand OpenSettingsCommand { get; }

		public MainWindowVm()
		{
			try
			{
				_logger = new Logger();
				_logger.LogInfo("Starting up...");
				_logger.LogInfo($"App: {Constants.AppHeaderString}");
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

				InitializeSettings();
				InitializeSubViewModels();
				InitializeIoDevices();

				OpenSettingsCommand = new CommandHandler(OpenSettings, true);

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

		private void ScannerListener_CharSequenceFormed(string charSequence)
		{
			_calculationDashboardControlVm?.UpdateCodeText(charSequence);
		}

		public void ExitApplication()
		{
			try
			{
				_logger.LogInfo("Shutting down...");

				SaveSettings();
				DisposeSubViewModels();
				DisposeIoDevices();

				_logger.LogInfo("Application stopped");
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
				_streamViewControlVm.ColorFrameReady += settingsWindowVm.ColorFrameUpdated;
				_streamViewControlVm.RawDepthFrameReady += settingsWindowVm.DepthFrameUpdated;
				var settingsWindow = new SettingsWindow
				{
					Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
					DataContext = settingsWindowVm
				};

				var settingsChanged = settingsWindow.ShowDialog() == true;
				_streamViewControlVm.ColorFrameReady -= settingsWindowVm.ColorFrameUpdated;
				_streamViewControlVm.RawDepthFrameReady -= settingsWindowVm.DepthFrameUpdated;

				if (!settingsChanged)
					return;

				Settings = settingsWindowVm.GetSettings();
				SaveSettings();
				_logger.LogInfo($"New settings have been applied: {Settings}");
			}
			catch (Exception ex)
			{
				_logger.LogException("Exception occured during a settings change", ex);
				MessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void InitializeSubViewModels()
		{
			_logger.LogInfo("Initializing sub view models...");

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

		private void InitializeIoDevices()
		{
			try
			{
				_keyboardListener = new KeyboardListener(_logger);
				_keyboardListener.CharSequenceFormed += ScannerListener_CharSequenceFormed;

				var scannerComPort = IoUtils.ReadScannerPort();
				if (scannerComPort == string.Empty)
					return;

				_serialListener = new SerialPortListener(_logger, scannerComPort);
				_serialListener.CharSequenceFormed += ScannerListener_CharSequenceFormed;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to initialize Io devices", ex);
			}
		}

		private void SaveSettings()
		{
			_logger.LogInfo("Saving settings...");
			IoUtils.SerializeSettings(Settings);
		}

		private void DisposeSubViewModels()
		{
			_logger.LogInfo("Disposing sub view models...");
			_streamViewControlVm.Dispose();
			_calculationDashboardControlVm.Dispose();
		}

		private void DisposeIoDevices()
		{
			_logger.LogInfo("Disposing io devices...");

			if (_keyboardListener != null)
				_keyboardListener.CharSequenceFormed -= ScannerListener_CharSequenceFormed;

			if (_serialListener == null)
				return;

			_serialListener.CharSequenceFormed -= ScannerListener_CharSequenceFormed;
			_serialListener.Dispose();
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