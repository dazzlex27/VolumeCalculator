using Primitives.Logging;
using VCClient.ViewModels;

namespace VolumeCalculatorTests.GUI
{
	[TestFixture]
	internal class MainWindowVmTest
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
		public async Task UpdateSettings_WhenCalledRightAfterServerStartup_DoesntThrow()
		{
			var server = await TestUtils.CreateServerAsync(_logger, _httpClient);
			var vm = new MainWindowVm(_logger, _httpClient, server);
			vm.UpdateSettings(server.GetSettings());
		}

		[Test]
		public async Task CalculationStartRequested_WhenTriggeredCalculation_RaisesEvent()
		{
			var server = await TestUtils.CreateServerAsync(_logger, _httpClient);
			var vm = new MainWindowVm(_logger, _httpClient, server);

			var isCalled = false;
			vm.CalculationStartRequested += () => isCalled = true;
			vm.StartMeasurementCommand.Execute(null);

			Assert.That(isCalled, Is.True);
		}

		[Test]
		public async Task Dispose_WhenCalledAfterServerStartup_NoThrow()
		{
			var server = await TestUtils.CreateServerAsync(_logger, _httpClient);
			
			var vm = new MainWindowVm(_logger, _httpClient, server);

			vm.Dispose();
		}

		[Test]
		public async Task ShutDownRequested_WhenTriggeredCalculation_RaisesEvent()
		{
			var gtShutDown = true;
			var gtForce = false;
			var server = await TestUtils.CreateServerAsync(_logger, _httpClient);
			var vm = new MainWindowVm(_logger, _httpClient, server);

			var isCalled = false;
			var shutDown = false;
			var force = false;
			vm.ShutDownRequested += (sd, f) =>
			{
				isCalled = true;
				shutDown = sd;
				force = f;
			};
			vm.ShutDownCommand.Execute(null);

			Assert.Multiple(() =>
			{
				Assert.That(isCalled, Is.True);
				Assert.That(shutDown, Is.EqualTo(gtShutDown));
				Assert.That(force, Is.EqualTo(gtForce));
			});
		}
	}
}
