using Primitives.Logging;
using VCClient.ViewModels;
using VCServer;

namespace VolumeCalculatorTests
{
	[TestFixture]
	internal class AppTest
	{
		ILogger _logger;
		HttpClient _httpClient;

		[SetUp]
		public void SetUp()
		{
			_logger = new DummyLogger();
			_httpClient = new HttpClient();
		}

		[TearDown]
		public void TearDown()
		{
			_httpClient?.Dispose();
			_logger?.Dispose();
		}

		[Test]
		public void MainWindowVmCtor_WhenObjectCreated_ReturnsNewObject()
		{
			var vm = new MainWindowVm(_logger, _httpClient, null);

			Assert.That(vm, Is.Not.Null);
		}

		[Test]
		public void MainWindowVmDispose_WhenNewlyCreatedObjectDisposed_NoThrow()
		{
			var vm = new MainWindowVm(_logger, _httpClient, null);

			vm.Dispose();

			Assert.That(vm, Is.Not.Null);
		}

		[Test]
		public async Task ConstructAndTearDownServerAndClient_WhenGivenServerObjectWithEmulatedDevices_NoThrow()
		{
			var components = await TestUtils.CreateAppAsync(_logger, _httpClient);

			components.Item1.Calculator.ValidateStatus();

			DisposeApp(components.Item1, components.Item2);
		}

		private void DisposeApp(ServerComponentsHandler server, MainWindowVm vm)
		{
			vm.Dispose();
			server.Dispose();
		}
	}
}
