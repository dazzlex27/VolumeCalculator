using ExtIntegration;
using FrameProcessor;
using CommonUtils;
using Primitives.Calculation;
using Primitives.Logging;
using Primitives.Settings;
using DeviceIntegration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VCServer.DeviceHandling;
using VCServer.VolumeCalculation;

namespace VCServer
{
	public sealed class ServerComponentsHandler : IDisposable
	{
		public event Action<ApplicationSettings> ApplicationSettingsChanged;

		private readonly ILogger _logger;
		private readonly HttpClient _httpClient;

		private ISettingsHandler _settingsHandler;
		private ApplicationSettings _settings;
		private RequestProcessor _requestProcessor;

		public ServerComponentsHandler(ILogger logger, HttpClient httpClient)
		{
			_logger = logger;
			_httpClient = httpClient;
		}

		public DeviceManager DeviceManager { get; private set; }

		public CalculationRequestHandler Calculator { get; private set; }

		public DepthMapProcessor DmProcessor { get; private set; }

		public void InitializeIoDevicesAsync(ILogger deviceLogger, DeviceFactory factory)
		{
			DeviceManager = new DeviceManager(deviceLogger, _httpClient, _settings.IoSettings, factory);
		}

		public void InitializeCalculationSystems(ILogger logger)
		{
			var frameProvider = DeviceManager.DeviceSet.FrameProvider;
			var colorCameraParams = frameProvider.GetColorCameraParams();
			var depthCameraParams = frameProvider.GetDepthCameraParams();

			DmProcessor = new DepthMapProcessor(logger, colorCameraParams, depthCameraParams);
			DmProcessor.SetProcessorSettings(_settings);

			Calculator = new CalculationRequestHandler(logger, DmProcessor, DeviceManager.DeviceSet);
			Calculator.UpdateSettings(_settings);
			Calculator.CalculationFinished += OnCalculationFinished;
			Calculator.CalculationStatusChanged += OnStatusChanged;
			DeviceManager.DeviceEventGenerator.BarcodeReady += Calculator.UpdateBarcode;
			DeviceManager.DeviceEventGenerator.WeightMeasurementReady += Calculator.UpdateWeight;
		}

		public async Task InitializeIntegrationsAsync(ILogger integrationLogger)
		{
			_requestProcessor = new RequestProcessor(integrationLogger, _httpClient,
				_settings.IntegrationSettings, _settings.GeneralSettings.OutputPath);
			_requestProcessor.StartRequestReceived += OnCalculationStartRequested;
			await _requestProcessor.StartAsync();
		}

		public async Task InitializeSettingsAsync(ISettingsHandler settingsHandler)
		{
			_settingsHandler = settingsHandler;
			var settingsFromHandler = await settingsHandler.LoadAsync();

			ApplicationSettings settings;
			if (settingsFromHandler == null)
			{
				_logger.LogError("Failed to read settings from handler, will use default settings");
#if DEBUG
				settings = ApplicationSettings.GetDefaultDebugSettings();
#else
				settings = ApplicationSettings.GetDefaultSettings();
#endif
			}
			else
				settings = settingsFromHandler;

			UpdateApplicationSettings(settings);
		}

		public void Dispose()
		{
			_settingsHandler.SaveAsync(_settings);
			DeviceManager?.Dispose();
			DisposeSubSystems();
		}

		public void DisposeSubSystems()
		{
			_logger?.LogInfo("Disposing sub systems...");

			Calculator?.Dispose();
			_requestProcessor?.Dispose();
			DmProcessor?.Dispose();

			_logger.LogInfo("Disposed subsystems");
		}

		public ApplicationSettings GetSettings()
		{
			return _settings;
		}

		public void UpdateApplicationSettings(ApplicationSettings settings)
		{
			_settings = settings;
			Calculator?.UpdateSettings(settings);
			DmProcessor?.SetProcessorSettings(settings);
			DeviceManager?.DeviceEventGenerator?.UpdateSettings(settings);
			_requestProcessor?.UpdateSettings(settings);

			ApplicationSettingsChanged?.Invoke(settings);

			_logger.LogInfo($"New settings have been applied: {settings}");
		}

		public async Task SaveSettingsAsync()
		{
			_logger.LogInfo("Saving settings...");
			await _settingsHandler.SaveAsync(_settings);
		}

		public void ShutPcDown()
		{
			IoUtils.ShutPcDown();
		}

		private void OnCalculationFinished(CalculationResultData resultData)
		{
			_requestProcessor.SendRequestsAsync(resultData);
		}

		private void OnCalculationStartRequested(CalculationRequestData data)
		{
			Calculator?.StartCalculation(data);
		}

		private void OnStatusChanged(CalculationStatus status)
		{
			_requestProcessor.UpdateCalculationStatus(status);
			DeviceManager.DeviceStateUpdater.UpdateCalculationStatus(status);
		}
	}
}
