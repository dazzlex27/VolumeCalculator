using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using ExtIntegration;
using FrameProcessor;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;
using VolumeCalculator.GUI;
using VolumeCalculator.Utils;

namespace VolumeCalculator
{
	internal class MainWindowVm : BaseViewModel
	{
		public event Action<CalculationRequestData> CalculationStartRequested;
		private event Action<ApplicationSettings> ApplicationSettingsChanged;

		private readonly ILogger _logger;
		private readonly List<string> _fatalErrorMessages;

		private ApplicationSettings _settings;
		private DeviceSet _deviceSet;
		private DepthMapProcessor _dmProcessor;
		private RequestProcessor _requestProcessor;
		private CalculationResultFileProcessor _calculationResultFileProcessor;

		private StreamViewControlVm _streamViewControlVm;
		private CalculationDashboardControlVm _calculationDashboardControlVm;
		private TestDataGenerationControlVm _testDataGenerationControlVm;

		private readonly HttpClient _httpClient;
		private bool _usingMasks;

		public string WindowTitle => GlobalConstants.AppHeaderString;

		public bool ShutDownInProgress { get; private set; }

		public StreamViewControlVm StreamViewControlVm
		{
			get => _streamViewControlVm;
			set => SetField(ref _streamViewControlVm, value, nameof(StreamViewControlVm));
		}

		public CalculationDashboardControlVm CalculationDashboardControlVm
		{
			get => _calculationDashboardControlVm;
			set => SetField(ref _calculationDashboardControlVm, value, nameof(CalculationDashboardControlVm));
		}

		public TestDataGenerationControlVm TestDataGenerationControlVm
		{
			get => _testDataGenerationControlVm;
			set => SetField(ref _testDataGenerationControlVm, value, nameof(TestDataGenerationControlVm));
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

		public bool ShutDownByDefault => _settings?.IoSettings != null && _settings.IoSettings.ShutDownPcByDefault;

		public ICommand OpenSettingsCommand { get; }

		public ICommand OpenStatusCommand { get; }

		public ICommand OpenConfiguratorCommand { get; }

		public ICommand ShutDownCommand { get; }

		public ICommand InitializeApplication { get; }

		public MainWindowVm()
		{
			try
			{
				_httpClient = new HttpClient();

				var maskBytes = DeviceSetFactory.GetMaskBytes();

				_fatalErrorMessages = new List<string>();
				_logger = new Logger();
				_logger.LogInfo($"Starting up \"{GlobalConstants.AppHeaderString}\"...");
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				InitializeSettings(maskBytes);
				InitializeIoDevices();
				InitializeSubSystems();
				InitializeSubViewModels();

				OpenSettingsCommand = new CommandHandler(OpenSettingsWindow, true);
				OpenStatusCommand = new CommandHandler(OpenStatusWindow, true);
				OpenConfiguratorCommand = new CommandHandler(OpenConfigurator, true);
				ShutDownCommand = new CommandHandler(() => { ShutDown(true, false); }, true);

                if (_usingMasks)
                    MessageBox.Show("Лицензия не установлена. Пожалуйста, установите лицензию.", "Ошибка лицензии",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                _logger.LogInfo("Application is initalized");
			}
			catch (Exception ex)
			{
				_logger.LogException("Application is terminating...", ex);
				DisplayFatalErrorsAndCloseApplication();
			}
		}

		public bool ShutDown(bool shutPcDown, bool force)
		{
			if (ShutDownInProgress)
				return true;

			ShutDownInProgress = true;

			try
			{
				if (force)
				{
					DisposeSubSystems();
					Process.GetCurrentProcess().Kill();
					return true;
				}

				if (MessageBox.Show("Вы действительно хотите отключить систему?", "Завершение работы",
						MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return false;

				_logger.LogInfo("Disposing the application...");

				SaveSettings();
				DisposeSubViewModels();
				DisposeSubSystems();
				DisposeIoDevices();

				_httpClient.Dispose();

				_logger.LogInfo("Application stopped");

				if (!shutPcDown)
					return true;

				_logger.LogInfo("Shutting down the system...");
				IoUtils.ShutPcDown();

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to close the application", ex);
				return false;
			}
			finally
			{
				ShutDownInProgress = false;
			}
		}

		private void InitializeSettings(byte[] maskBytes)
		{
			try
			{
				_logger.LogInfo("Reading settings...");
				var settingsFromFile = IoUtils.DeserializeSettings();
				if (settingsFromFile == null)
				{
					_logger.LogError("Failed to read settings from file, will use default settings");
					Settings = ApplicationSettings.GetDefaultSettings();
					IoUtils.SerializeSettings(Settings);
				}
				else
					Settings = settingsFromFile;

				_usingMasks = StreamViewControlVm.CheckIfOk(maskBytes);

				_logger.LogInfo("Settings - ok");
			}
			catch (Exception ex)
			{
				_logger.LogException("FATAL: Failed to initialize application settings!", ex);

				var message = "Не удалось инициализировать настройки приложения";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void InitializeIoDevices()
		{
			try
			{
				_logger.LogInfo("Initializing IO devices...");

				var deviceSetFaceory = new DeviceSetFactory();
				_deviceSet = deviceSetFaceory.CreateDeviceSet(_logger, _httpClient, Settings.IoSettings);

				_logger.LogInfo("IO devices- ok");
			}
			catch (Exception ex)
			{
				_logger.LogException("FATAL: Failed to initialize IO devices!", ex);

				var message = "Не удалось инициализировать внешние устройства";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void InitializeSubSystems()
		{
			try
			{
				_logger.LogInfo("Initializing sub systems...");

				var frameProvider = _deviceSet.FrameProvider;

				var colorCameraParams = frameProvider.GetColorCameraParams();
				var depthCameraParams = frameProvider.GetDepthCameraParams();

				_dmProcessor = new DepthMapProcessor(_logger, colorCameraParams, depthCameraParams);
				_dmProcessor.SetProcessorSettings(Settings, _usingMasks);

				_requestProcessor = new RequestProcessor(_logger, _httpClient, _settings.IntegrationSettings);
				_requestProcessor.StartRequestReceived += OnCalculationStartRequested;

				var outputPath = _settings.IoSettings.OutputPath;
				_calculationResultFileProcessor = new CalculationResultFileProcessor(_logger, outputPath);

				_logger.LogInfo("Sub systems - ok");
			}
			catch (Exception ex)
			{
				_logger.LogException("FATAL: Failed to initialize sub systems!", ex);

				var message = "Не удалось инициализировать дополнительные подсистемы";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void OnCalculationStartRequested(CalculationRequestData data)
		{
			CalculationStartRequested?.Invoke(data);
		}

		private void InitializeSubViewModels()
		{
			try
			{
				_logger.LogInfo("Initializing GUI handlers...");

				var frameProvider = _deviceSet.FrameProvider;

				_streamViewControlVm = new StreamViewControlVm(_logger, _settings, frameProvider);

				_calculationDashboardControlVm =
					new CalculationDashboardControlVm(_logger, _settings, _deviceSet, _dmProcessor);
				_calculationDashboardControlVm.CalculationFinished += OnCalculationFinished;
				_calculationDashboardControlVm.CalculationStatusChanged += OnCalculationStatusChanged;
				CalculationStartRequested += _calculationDashboardControlVm.StartCalculation;
				if (_usingMasks)
					_logger.LogInfo("Additional masks are on...");

				_testDataGenerationControlVm = new TestDataGenerationControlVm(_logger, _settings, frameProvider);

				ApplicationSettingsChanged += OnApplicationSettingsChanged;

				_logger.LogInfo("GUI handlers - ok");
			}
			catch (Exception ex)
			{
				_logger.LogException("FATAL: Failed to initialize GUI!", ex);

				var message = "Не удалось инициализировать основные программные компоненты системы";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void OnCalculationStatusChanged(CalculationStatus status)
		{
			_requestProcessor.UpdateCalculationStatus(status);
		}

		private void OnCalculationFinished(CalculationResultData resultData)
		{
			_requestProcessor.SendRequests(resultData);
			_calculationResultFileProcessor.WriteCalculationResult(resultData);
		}

		private void SaveSettings()
		{
			_logger.LogInfo("Saving settings...");
			IoUtils.SerializeSettings(Settings);
		}

		private void OpenSettingsWindow()
		{
			try
			{
				_deviceSet.TogglePause(true);

				var settingsWindowVm = new SettingsWindowVm(_logger, _settings, _deviceSet.FrameProvider.GetDepthCameraParams(),
					_dmProcessor);
				_deviceSet.FrameProvider.ColorFrameReady += settingsWindowVm.ColorFrameUpdated;
				_deviceSet.FrameProvider.DepthFrameReady += settingsWindowVm.DepthFrameUpdated;
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
					_deviceSet.TogglePause(false);

					_deviceSet.FrameProvider.ColorFrameReady -= settingsWindowVm.ColorFrameUpdated;
					_deviceSet.FrameProvider.DepthFrameReady -= settingsWindowVm.DepthFrameUpdated;
				}
			}
			catch (Exception ex)
			{
				_logger.LogException("Exception occured during a settings change", ex);
				AutoClosingMessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка");
			}

			if (_usingMasks)
				throw new AbandonedMutexException();
		}

		private void OpenStatusWindow()
		{
			try
			{
				var statusWindowVm = new StatusWindowVm(_logger, _httpClient, !_usingMasks);

				var statusWindow = new StatusWindow
				{
					Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
					DataContext = statusWindowVm
				};

				statusWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				_logger.LogException("Error occured while displaying status window", ex);
			}
		}

		private void OpenConfigurator()
		{
			try
			{
				var message = "Данное действие приведёт к закрытию текущего приложения и открытию приложения настройки, вы хотите продолжить?";

				if (MessageBox.Show(message, "Открытие Конфигуратора", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return;

				IoUtils.StartProcess("VCConfigurator.exe", true);
				ShutDown(false, true);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to launch configurator", ex);
			}
		}

		private void DisposeSubViewModels()
		{
			_logger.LogInfo("Disposing sub view models...");
			_dmProcessor?.Dispose();

			_streamViewControlVm?.Dispose();
			_calculationDashboardControlVm?.Dispose();
		}

		private void DisposeSubSystems()
		{
			_logger.LogInfo("Disposing sub systems...");

			_requestProcessor?.Dispose();
		}

		private void DisposeIoDevices()
		{
			_logger.LogInfo("Disposing io devices...");

			_deviceSet?.Dispose();
		}

		private void OnApplicationSettingsChanged(ApplicationSettings settings)
		{
			_dmProcessor.SetProcessorSettings(settings, _usingMasks);
			_calculationDashboardControlVm.ApplicationSettingsUpdated(settings);
			_streamViewControlVm.ApplicationSettingsUpdated(settings);
			_testDataGenerationControlVm.ApplicationSettingsUpdated(settings);
			_calculationResultFileProcessor.UpdateSettings(settings);
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);

			DisplayFatalErrorsAndCloseApplication();
		}

		private void DisplayFatalErrorsAndCloseApplication()
		{
			var builder = new StringBuilder();
			builder.AppendLine("Произошли критические ошибки:");
			foreach (var error in _fatalErrorMessages)
				builder.AppendLine(error);
			builder.AppendLine();
			builder.AppendLine("Приложение будет закрыто, информация записана в журнал");

			AutoClosingMessageBox.Show(builder.ToString(), "Аварийное завершение");

			var settingsAreOk = Settings?.IoSettings != null;
			var needToshutDownPc = !settingsAreOk || Settings.IoSettings.ShutDownPcByDefault;
			ShutDown(needToshutDownPc, true);
		}
	}
}