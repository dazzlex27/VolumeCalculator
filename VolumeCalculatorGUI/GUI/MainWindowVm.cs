using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DeviceIntegrations.IoCircuits;
using DeviceIntegrations.Scales;
using DeviceIntegrations.Scanners;
using FrameProcessor;
using FrameProviders;
using Primitives;
using Primitives.Logging;
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

		private FrameProvider _frameProvider;
		private DepthMapProcessor _processor;
		private List<IBarcodeScanner> _barcodeScanners;
		private IScales _scales;
		private IIoCircuit _circuit;

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

		public ICommand ShutDownCommand { get; }

		public MainWindowVm()
		{
			try
			{
				_logger = new Logger();
				_logger.LogInfo("Starting up...");
				_logger.LogInfo($"App: {Constants.AppHeaderString}");
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				InitializeSettings();
				InitializeEntities();
				InitializeIoDevices();
				InitializeSubViewModels();

				OpenSettingsCommand = new CommandHandler(OpenSettings, true);
				ShutDownCommand = new CommandHandler(() => { ShutDown(true); }, true);

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

		private void InitializeEntities()
		{
			_frameProvider = DeviceInitializationUtils.CreateRequestedFrameProvider(_logger);
			_frameProvider.ColorCameraFps = 5;
			_frameProvider.DepthCameraFps = 5;
			_frameProvider.ColorFrameReady += OnColorFrameReady;
			_frameProvider.DepthFrameReady += OnDepthFrameReady;
			_frameProvider.Start();

			var colorCameraParams = _frameProvider.GetColorCameraParams();
			var depthCameraParams = _frameProvider.GetDepthCameraParams();

			_processor = new DepthMapProcessor(_logger, colorCameraParams, depthCameraParams);
			_processor.SetProcessorSettings(Settings);
		}

		public bool ShutDown(bool shutPcDown)
		{
			if (MessageBox.Show("Вы действительно хотите отключить систему?", "Завершение работы", MessageBoxButton.YesNo,
				    MessageBoxImage.Question) == MessageBoxResult.No)
			{
				return false;
			}

			try
			{
				_logger.LogInfo("Disposing the application...");

				SaveSettings();
				DisposeSubViewModels();
				DisposeIoDevices();

				if (!shutPcDown)
					return true;

				_logger.LogInfo("Application stopped, shutting down the system...");
				IoUtils.ShutPcDown();

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to close the application", ex);
				return false;
			}
		}

		private void OpenSettings()
		{
			try
			{
				var settingsWindowVm = new SettingsWindowVm(_logger, _settings, _frameProvider.GetDepthCameraParams(), _processor);
				_frameProvider.ColorFrameReady += settingsWindowVm.ColorFrameUpdated;
				_frameProvider.DepthFrameReady += settingsWindowVm.DepthFrameUpdated;
				try
				{
					var settingsWindow = new SettingsWindow
					{
						Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
						DataContext = settingsWindowVm
					};

					var settingsChanged = settingsWindow.ShowDialog() == true;
					if (!settingsChanged)
						return;

					Settings = settingsWindowVm.GetSettings();
					SaveSettings();
					_logger.LogInfo($"New settings have been applied: {Settings}");
				}
				catch (Exception ex)
				{
					_logger.LogException("Error occured while changing settings", ex);
				}
				finally
				{
					_frameProvider.ColorFrameReady -= settingsWindowVm.ColorFrameUpdated;
					_frameProvider.DepthFrameReady -= settingsWindowVm.DepthFrameUpdated;
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Exception occured during a settings change", ex);
				MessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
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

		private void InitializeSubViewModels()
		{
			_logger.LogInfo("Initializing sub view models...");

			_streamViewControlVm = new StreamViewControlVm(_logger, _settings, _frameProvider);

			_calculationDashboardControlVm = new CalculationDashboardControlVm(_logger, _frameProvider, _processor,
				_barcodeScanners, _scales, _circuit, _settings);
			_testDataGenerationControlVm = new TestDataGenerationControlVm(_settings, _frameProvider.GetDepthCameraParams());

			ApplicationSettingsChanged += OnApplicationSettingsChanged;
		}

		private void InitializeIoDevices()
		{
			try
			{
				_barcodeScanners = new List<IBarcodeScanner>();

				var keyboardListener = new GenericKeyboardBarcodeScanner(_logger);
				_barcodeScanners.Add(keyboardListener);
				var keyEventHandler = new KeyEventHandler(keyboardListener.AddKey);
				EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyUpEvent, keyEventHandler, true);

				var scannerComPort = IoUtils.ReadScannerPort();
				if (scannerComPort != string.Empty)
				{
					var serialListener = new GenericSerialPortBarcodeScanner(_logger, scannerComPort);
					_barcodeScanners.Add(serialListener);
				}

				var scalesComPort = IoUtils.ReadScalesPort();
				if (scalesComPort != string.Empty)
					_scales = DeviceInitializationUtils.CreateRequestedScales(_logger, scalesComPort);

				_logger.LogInfo("reading board port");
				var circuitComPort = IoUtils.ReadIoBoardPort();
				if (circuitComPort != string.Empty)
				{
					_logger.LogInfo("board port is not null");
					_circuit = new KeUsb24RBoard(_logger, circuitComPort);
				}
				else
				{
					_logger.LogInfo("board port is null");
				}
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

			foreach (var listener in _barcodeScanners.Where(l => l != null))
				listener.Dispose();

			_scales?.Dispose();

			_circuit?.Dispose();
		}

		private void OnApplicationSettingsChanged(ApplicationSettings settings)
		{
			_processor.SetProcessorSettings(settings);
			_calculationDashboardControlVm.ApplicationSettingsUpdated(settings);
			_streamViewControlVm.ApplicationSettingsUpdated(settings);
			_testDataGenerationControlVm.ApplicationSettingsUpdated(settings);
		}

		private void OnColorFrameReady(ImageData image)
		{
			_testDataGenerationControlVm?.ColorFrameUpdated(image);
		}

		private void OnDepthFrameReady(DepthMap depthMap)
		{
			_testDataGenerationControlVm?.DepthFrameUpdated(depthMap);
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);
			MessageBox.Show("Произошла критическая ошибка, приложение будет закрыто. Информация записана в журнал",
				"Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}