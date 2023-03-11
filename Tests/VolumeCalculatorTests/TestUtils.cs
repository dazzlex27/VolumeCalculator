using CommonUtils.Plugins;
using CommonUtils.SettingsLoaders;
using DeviceIntegration;
using FrameProviders;
using Primitives;
using Primitives.Calculation;
using Primitives.Logging;
using Primitives.Settings;
using VCClient.ViewModels;
using VCServer;

namespace VolumeCalculatorTests
{
	internal class TestUtils
	{
		public static ColorCameraParams GetDummyColorCameraParams()
		{
			return new ColorCameraParams(1, 1, 1, 1, 1, 1);
		}

		public static DepthCameraParams GetDummyDepthCameraParams()
		{
			return new DepthCameraParams(1, 1, 1, 1, 1, 1, 1, 1);
		}

		public static CalculationResultData GetSuccessfulResult()
		{
			var result = new CalculationResult(DateTime.Now, "test", 524, WeightUnits.Gr,
				0, 45, 37, 53, 88245, "test", false);
			var imageData = new ImageData(40, 30, 3);

			return new CalculationResultData(result, CalculationStatus.Successful, imageData);
		}

		public static ApplicationSettings GetSettingsWithEverythingDisabled()
		{
			var settings = ApplicationSettings.GetDefaultDebugSettings();
			settings.IntegrationSettings.DisableAllIntegrations();

			return settings;
		}

		public static async Task<Tuple<ServerComponentsHandler, MainWindowVm>> CreateAppAsync(
			ILogger logger, HttpClient httpClient)
		{
			var server = await CreateServerAsync(logger, httpClient);

			var vm = new MainWindowVm(logger, httpClient, server);
			vm.UpdateSettings(server.GetSettings());

			return new Tuple<ServerComponentsHandler, MainWindowVm>(server, vm);
		}

		public static async Task<ServerComponentsHandler> CreateServerAsync(
			ILogger logger, HttpClient httpClient)
		{
			var server = new ServerComponentsHandler(logger, httpClient);

			var settings = GetSettingsWithEverythingDisabled();
			var settingsHandler = new LocalSettingsHandler(settings);
			await server.InitializeSettingsAsync(settingsHandler);

			var deviceFactory = GetDeviceFactory();
			server.InitializeIoDevicesAsync(logger, deviceFactory);
			server.InitializeCalculationSystems(logger);
			await server.InitializeIntegrationsAsync(logger);

			return server;
		}

		private static DeviceFactory GetDeviceFactory()
		{
			// TODO: load plugins from a mock instead
			var definitions = new DeviceDefinitions();
			var registrator = new DeviceRegistrator(definitions);
			var toolset = new PluginToolset(registrator);
			var pluginContainer = new PluginFromDiskContainer(toolset, GlobalConstants.PluginsFolder);
			pluginContainer.LoadPlugins();

			return new DeviceFactory(definitions);
		}
	}
}
