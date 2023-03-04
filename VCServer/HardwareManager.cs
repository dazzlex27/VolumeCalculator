using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net.Http;
using DeviceIntegration.Cameras;
using DeviceIntegration.FrameProviders;
using DeviceIntegration.IoCircuits;
using DeviceIntegration.RangeMeters;
using DeviceIntegration.Scales;
using Primitives;
using Primitives.Logging;
using Primitives.Settings;

namespace VCServer
{
	public sealed class HardwareManager : IDisposable
	{
		public event Action<ScaleMeasurementData> WeightMeasurementReady;
		public event Action<string> BarcodeReady;

		[ImportMany]
		private IEnumerable<IPlugin> Plugins { get; set; }

		private readonly ILogger _logger;
		private readonly HttpClient _httpClient;
		private readonly IoSettings _ioSettings;

		private DeviceSet _deviceSet;
		private StateController _stateController;

		private bool _subtractPalletWeight;
		private double _palletWeightGr;

		public IFrameProvider FrameProvider => _deviceSet.FrameProvider;

		public IIoCircuit IoCircuit => _deviceSet.IoCircuit;

		public IRangeMeter RangeMeter => _deviceSet.RangeMeter;

		public IIpCamera IpCamera => _deviceSet.IpCamera;

		public HardwareManager(ILogger logger, HttpClient httpClient, IoSettings settings)
		{
			_logger = logger;
			_httpClient = httpClient;
			_ioSettings = settings;
		}

		public void CreateDevices()
		{
			_logger.LogInfo("Creating device manager...");
			_deviceSet = DeviceSetFactory.CreateDeviceSet(_logger, _httpClient, _ioSettings);

			if (_deviceSet.Scanners != null)
			{
				foreach (var scanner in _deviceSet.Scanners.Where(s => s != null))
					scanner.CharSequenceFormed += OnBarcodeReady;
			}

			_stateController = new StateController(IoCircuit, RangeMeter);

			if (_deviceSet.Scales != null)
				_deviceSet.Scales.MeasurementReady += OnScalesWeightReady;

			_logger.LogInfo("Device manager created");
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing device manager...");

			_stateController.Dispose();

			var scanners = _deviceSet.Scanners;
			if (scanners != null)
			{
				foreach (var scanner in scanners.Where(s => s != null))
					scanner.CharSequenceFormed -= OnBarcodeReady;
			}

			var scales = _deviceSet.Scales;
			if (scales != null)
				scales.MeasurementReady -= OnScalesWeightReady;

			_logger.LogInfo("Device manager disposed");
		}

		public void LoadPlugins()
		{
			var catalog = new DirectoryCatalog("Plugins");
			using var container = new CompositionContainer(catalog);
			container.ComposeParts(this);

			foreach (var plugin in Plugins)
				plugin.Initialize();
		}

		public void TogglePause(bool pause)
		{
			_deviceSet.TogglePause(pause);
		}

		public void ResetWeight()
		{
			_deviceSet.Scales?.ResetWeight();
		}

		public void UpdateSettings(ApplicationSettings settings)
		{
			_subtractPalletWeight = settings.AlgorithmSettings.EnablePalletSubtraction;
			_palletWeightGr = settings.AlgorithmSettings.PalletWeightGr;
		}

		public void UpdateCalculationStatus(CalculationStatus status)
		{
			_stateController.Update(status);
		}

		private void OnScalesWeightReady(ScaleMeasurementData data)
		{
			OnWeightMeasurementReady(data);
		}

		private void OnWeightMeasurementReady(ScaleMeasurementData data)
		{
			if (!_subtractPalletWeight)
				WeightMeasurementReady?.Invoke(data);
			else
			{
				var newData = new ScaleMeasurementData(data.Status, data.WeightGr - _palletWeightGr);
				WeightMeasurementReady?.Invoke(newData);
			}
		}

		private void OnBarcodeReady(string barcode)
		{
			BarcodeReady?.Invoke(barcode);
		}
	}
}
