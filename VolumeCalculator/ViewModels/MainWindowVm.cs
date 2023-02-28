using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DeviceIntegration.Scales;
using ExtIntegration;
using FrameProcessor;
using GuiCommon;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;
using ProcessingUtils;
using VCServer;
using VolumeCalculator.GUI;

namespace VolumeCalculator.ViewModels
{
	internal class MainWindowVm : BaseViewModel
	{
		private event Action<ApplicationSettings> ApplicationSettingsChanged;

		private readonly HttpClient _httpClient;
		private readonly ILogger _logger;
		private readonly List<string> _fatalErrorMessages;

		private ApplicationSettings _settings;
		private CalculationRequestHandler _calculator;
		private DepthMapProcessor _dmProcessor;
		private RequestProcessor _requestProcessor;
		private HardwareManager _deviceManager;
		private CalculationResultFileProcessor _calculationResultFileProcessor;

		private StreamViewControlVm _streamViewControlVm;
		private CalculationDashboardControlVm _dashboardControlVm;
		private TestDataGenerationControlVm _testDataGenerationControlVm;
		
		private volatile bool _shutDownInProgress;
		
		public string WindowTitle => GlobalConstants.AppHeaderString;

		public StreamViewControlVm StreamViewControlVm
		{
			get => _streamViewControlVm;
			set => SetField(ref _streamViewControlVm, value, nameof(StreamViewControlVm));
		}

		public CalculationDashboardControlVm CalculationDashboardControlVm
		{
			get => _dashboardControlVm;
			set => SetField(ref _dashboardControlVm, value, nameof(CalculationDashboardControlVm));
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

		public bool ShutDownByDefault => _settings?.IoSettings != null && _settings.GeneralSettings.ShutDownPcByDefault;

		public ICommand OpenSettingsCommand { get; }

		public ICommand OpenStatusCommand { get; }

		public ICommand OpenConfiguratorCommand { get; }

		public ICommand ShutDownCommand { get; }

		public ICommand StartMeasurementCommand { get; }
		
		public MainWindowVm()
		{
			try
			{
				_logger = new TxtLogger("Client", "main");
				_logger.LogInfo($"Starting up \"{GlobalConstants.AppHeaderString}\"...");
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

				// TODO: AsyncCommand (replace async void)
				// TODO: producer-consumer based logging
				_fatalErrorMessages = new List<string>();
				OpenSettingsCommand = new CommandHandler(OpenSettingsWindowAsync, true);
				OpenStatusCommand = new CommandHandler(OpenStatusWindowAsync, true);
				OpenConfiguratorCommand = new CommandHandler(OpenConfiguratorAsync, true);
				ShutDownCommand = new CommandHandler(() => { ShutDownAsync(true, false); }, true);
				StartMeasurementCommand = new CommandHandler(() => { OnCalculationStartRequested(null); }, true);

				_httpClient = new HttpClient();

				// TODO: async startup
				InitializeSettingsAsync();
				InitializeIoDevicesAsync();
				InitializeSubSystemsAsync();
				InitializeSubViewModelsAsync();

				_logger.LogInfo("Application is initalized");
			}
			catch (Exception ex)
			{
				_logger.LogException("Application is terminating...", ex);
				DisplayFatalErrorsAndCloseApplication();
			}
		}

		public async Task<bool> ShutDownAsync(bool shutPcDown, bool force)
		{
			if (_shutDownInProgress)
				return true;

			_shutDownInProgress = true;

			try
			{
				if (force)
				{
					await DisposeSubSystemsAsync();
					Process.GetCurrentProcess().Kill();
					return true;
				}

				if (MessageBox.Show("Вы действительно хотите отключить систему?", "Завершение работы",
						MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return false;

				await _logger.LogInfo("Disposing the application...");

				await SaveSettingsAsync();
				await DisposeSubViewModelsAsync();
				await DisposeSubSystemsAsync();
				
				_deviceManager.Dispose();
				_httpClient.Dispose();

				await _logger.LogInfo("Application stopped");

				if (!shutPcDown)
					return true;

				await _logger.LogInfo("Shutting down the system...");
				IoUtils.ShutPcDown();

				return true;
			}
			catch (Exception ex)
			{
				await _logger.LogException("Failed to close the application", ex);
				return false;
			}
			finally
			{
				_shutDownInProgress = false;
			}
		}

		private async Task InitializeSettingsAsync()
		{
			try
			{
				await _logger.LogInfo("Reading settings...");
				var settingsFromFile = await IoUtils.DeserializeSettingsAsync<ApplicationSettings>();
				if (settingsFromFile == null)
				{
					await _logger.LogError("Failed to read settings from file, will use default settings");
					Settings = ApplicationSettings.GetDefaultSettings();
					await IoUtils.SerializeSettingsAsync(Settings);
				}
				else
					Settings = settingsFromFile;

				await _logger.LogInfo("Settings - ok");
			}
			catch (Exception ex)
			{
				await _logger.LogException("FATAL: Failed to initialize application settings!", ex);

				_fatalErrorMessages.Add("Не удалось инициализировать настройки приложения");
				throw;
			}
		}

		private async Task InitializeIoDevicesAsync()
		{
			try
			{
				await _logger.LogInfo("Initializing IO devices...");
				var deviceLogger = new TxtLogger("Client", "devices");

				_deviceManager = new HardwareManager(deviceLogger, _httpClient, Settings.IoSettings);
				_deviceManager.BarcodeReady += OnBarcodeReady;
				_deviceManager.WeightMeasurementReady += OnWeightMeasurementReady;

				await _logger.LogInfo("IO devices- ok");
			}
			catch (Exception ex)
			{
				await _logger.LogException("FATAL: Failed to initialize IO devices!", ex);

				var message = "Не удалось инициализировать внешние устройства";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private async Task InitializeSubSystemsAsync()
		{
			try
			{
				await _logger.LogInfo("Initializing sub systems...");

				var frameProvider = _deviceManager.FrameProvider;

				var colorCameraParams = frameProvider.GetColorCameraParams();
				var depthCameraParams = frameProvider.GetDepthCameraParams();

				_dmProcessor = new DepthMapProcessor(_logger, colorCameraParams, depthCameraParams);
				_dmProcessor.SetProcessorSettings(Settings);

				var integrationLogger = new TxtLogger("Client", "integration");
				_requestProcessor = new RequestProcessor(integrationLogger, _httpClient, _settings.IntegrationSettings);
				_requestProcessor.StartRequestReceived += OnCalculationStartRequested;
				await _requestProcessor.StartAsync();
				var outputPath = _settings.GeneralSettings.OutputPath;
				_calculationResultFileProcessor = new CalculationResultFileProcessor(integrationLogger, outputPath);

				_calculator = new CalculationRequestHandler(_logger, _dmProcessor, _deviceManager);
				_calculator.UpdateSettings(Settings);
				_calculator.CalculationFinished += OnCalculationFinished;
				_calculator.CalculationStatusChanged += OnStatusChanged;

				await _logger.LogInfo("Sub systems - ok");
			}
			catch (Exception ex)
			{
				await _logger.LogException("FATAL: Failed to initialize sub systems!", ex);

				var message = "Не удалось инициализировать дополнительные подсистемы";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private async Task InitializeSubViewModelsAsync()
		{
			try
			{
				await _logger.LogInfo("Initializing GUI handlers...");

				var frameProvider = _deviceManager.FrameProvider;

				_streamViewControlVm = new StreamViewControlVm(_logger, _settings, frameProvider);

				_dashboardControlVm = new CalculationDashboardControlVm(_logger);
				_dashboardControlVm.UpdateSettings(_settings);
				_dashboardControlVm.WeightResetRequested += _deviceManager.ResetWeight;
				_deviceManager.WeightMeasurementReady += _dashboardControlVm.UpdateWeight;
				_deviceManager.BarcodeReady += _dashboardControlVm.UpdateBarcode;
				_dashboardControlVm.CalculationCancellationRequested += _calculator.CancelPendingCalculation;
				_dashboardControlVm.CalculationRequested += OnCalculationStartRequested;
				_dashboardControlVm.LockingStatusChanged += _calculator.UpdateLockingStatus;
				_calculator.LastAlgorithmUsedChanged += _dashboardControlVm.UpdateLastAlgorithm;
				_calculator.ValidateStatus();

				_testDataGenerationControlVm = new TestDataGenerationControlVm(_logger, _settings, frameProvider);

				ApplicationSettingsChanged += OnApplicationSettingsChanged;

				await _logger.LogInfo("GUI handlers - ok");
			}
			catch (Exception ex)
			{
				await _logger.LogException("FATAL: Failed to initialize GUI!", ex);

				var message = "Не удалось инициализировать основные программные компоненты системы";
				_fatalErrorMessages.Add(message);
				throw;
			}
		}

		private void OnBarcodeReady(string barcode)
		{
			_calculator?.UpdateBarcode(barcode);
			_dashboardControlVm?.UpdateBarcode(barcode);
		}
		
		private void OnWeightMeasurementReady(ScaleMeasurementData data)
		{
			_calculator?.UpdateWeight(data);
			_dashboardControlVm?.UpdateWeight(data);
		}

		private void OnCalculationFinished(CalculationResultData resultData)
		{
			_requestProcessor.SendRequestsAsync(resultData);
			_calculationResultFileProcessor.WriteCalculationResult(resultData);
			_dashboardControlVm.UpdateDataUponCalculationFinish(resultData);
		}

		private void OnCalculationStartRequested(CalculationRequestData data)
		{
			_calculator.StartCalculation(data);
		}

		private void OnStatusChanged(CalculationStatus status)
		{
			_requestProcessor.UpdateCalculationStatus(status);
			_dashboardControlVm.UpdateCalculationStatus(status);
			_deviceManager.UpdateCalculationStatus(status);
		}

		private async Task SaveSettingsAsync()
		{
			await _logger.LogInfo("Saving settings...");
			await IoUtils.SerializeSettingsAsync(Settings);
		}

		private async void OpenSettingsWindowAsync()
		{
			try
			{
				_deviceManager.TogglePause(true);

				var settingsWindowVm = new SettingsWindowVm(_logger, _settings, _deviceManager.FrameProvider.GetDepthCameraParams(),
					_dmProcessor);
				_deviceManager.FrameProvider.ColorFrameReady += settingsWindowVm.ColorFrameUpdated;
				_deviceManager.FrameProvider.DepthFrameReady += settingsWindowVm.DepthFrameUpdated;
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
					await SaveSettingsAsync();
					await _logger.LogInfo($"New settings have been applied: {Settings}");
				}
				catch (Exception ex)
				{
					await _logger.LogException("Error occured while changing settings", ex);
				}
				finally
				{
					_deviceManager.TogglePause(false);

					_deviceManager.FrameProvider.ColorFrameReady -= settingsWindowVm.ColorFrameUpdated;
					_deviceManager.FrameProvider.DepthFrameReady -= settingsWindowVm.DepthFrameUpdated;
				}
			}
			catch (Exception ex)
			{
				await _logger.LogException("Exception occured during a settings change", ex);
				AutoClosingMessageBox.Show("Во время задания настроек произошла ошибка. Информация записана в журнал", "Ошибка");
			}
		}

		private async void OpenStatusWindowAsync()
		{
			try
			{
				var statusWindowVm = new StatusWindowVm(_logger, _httpClient);

				var statusWindow = new StatusWindow
				{
					Owner = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive),
					DataContext = statusWindowVm
				};

				statusWindow.ShowDialog();
			}
			catch (Exception ex)
			{
				await _logger.LogException("Error occured while displaying status window", ex);
			}
		}

		private async void OpenConfiguratorAsync()
		{
			try
			{
				var message = "Данное действие приведёт к закрытию текущего приложения и открытию приложения настройки, вы хотите продолжить?";

				if (MessageBox.Show(message, "Открытие Конфигуратора", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
					return;

				IoUtils.StartProcess("VCConfigurator.exe", true);
				await ShutDownAsync(false, true);
			}
			catch (Exception ex)
			{
				await _logger.LogException("Failed to launch configurator", ex);
			}
		}

		private async Task DisposeSubViewModelsAsync()
		{
			await _logger.LogInfo("Disposing sub view models...");
			
			_streamViewControlVm?.Dispose();
		}

		private async Task DisposeSubSystemsAsync()
		{
			await _logger.LogInfo("Disposing sub systems...");
			
			_calculator?.Dispose();
			_requestProcessor?.Dispose();
			_dmProcessor?.Dispose();
		}

		private void OnApplicationSettingsChanged(ApplicationSettings settings)
		{
			_calculator.UpdateSettings(settings);
			_dmProcessor.SetProcessorSettings(settings);
			_deviceManager.UpdateSettings(settings);
			_dashboardControlVm.UpdateSettings(settings);
			_streamViewControlVm.UpdateSettings(settings);
			_testDataGenerationControlVm.UpdateSettings(settings);
			_calculationResultFileProcessor.UpdateSettings(settings);
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_logger.LogException("Unhandled exception in application domain occured, app terminates...",
				(Exception)e.ExceptionObject);

			DisplayFatalErrorsAndCloseApplication();
		}

		private async Task DisplayFatalErrorsAndCloseApplication()
		{
			var builder = new StringBuilder();
			builder.AppendLine("Произошли критические ошибки:");
			foreach (var error in _fatalErrorMessages)
				builder.AppendLine(error);
			builder.AppendLine();
			builder.AppendLine("Приложение будет закрыто, информация записана в журнал");

			AutoClosingMessageBox.Show(builder.ToString(), "Аварийное завершение", 5000);

			var settingsAreOk = Settings?.IoSettings != null;
			var needToshutDownPc = !settingsAreOk || Settings.GeneralSettings.ShutDownPcByDefault;
			await ShutDownAsync(needToshutDownPc, true);
		}
	}
}
