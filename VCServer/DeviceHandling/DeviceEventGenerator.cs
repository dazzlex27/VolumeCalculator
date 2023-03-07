using DeviceIntegration;
using DeviceIntegration.Scales;
using Primitives.Settings;
using System;
using System.Linq;

namespace VCServer.DeviceHandling
{
	public sealed class DeviceEventGenerator : IDisposable
	{
		public event Action<ScaleMeasurementData> WeightMeasurementReady;
		public event Action<string> BarcodeReady;

		private readonly DeviceSet _deviceSet;

		private bool _subtractPalletWeight;
		private double _palletWeightGr;

		public DeviceEventGenerator(DeviceSet deviceSet)
		{
			_deviceSet = deviceSet;

			var scanners = _deviceSet.Scanners;
			if (scanners != null)
			{
				foreach (var scanner in scanners.Where(s => s != null))
					scanner.CharSequenceFormed += OnBarcodeReady;
			}

			var scales = _deviceSet.Scales;
			if (scales != null)
				scales.MeasurementReady += OnScalesWeightReady;
		}

		public void Dispose()
		{
			var scanners = _deviceSet.Scanners;
			if (scanners != null)
			{
				foreach (var scanner in scanners.Where(s => s != null))
					scanner.CharSequenceFormed -= OnBarcodeReady;
			}

			var scales = _deviceSet.Scales;
			if (scales != null)
				scales.MeasurementReady -= OnScalesWeightReady;
		}

		public void UpdateSettings(ApplicationSettings settings)
		{
			_subtractPalletWeight = settings.AlgorithmSettings.EnablePalletSubtraction;
			_palletWeightGr = settings.AlgorithmSettings.PalletWeightGr;
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
