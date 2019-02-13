using System;
using System.Threading;
using System.Threading.Tasks;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class FakeScales : IScales
	{
		private readonly CancellationTokenSource _tokenSource;

		public event Action<ScaleMeasurementData> MeasurementReady;

		private volatile bool _applyPayload;

		public FakeScales(ILogger logger)
		{
			_tokenSource = new CancellationTokenSource();

			Task.Run(async () => {
				try
				{
					var statusSwitchTimer = new System.Timers.Timer(10000) { AutoReset = true };
					statusSwitchTimer.Elapsed += OnstatusSwitchTimerElapsed;
					statusSwitchTimer.Start();

					while (!_tokenSource.IsCancellationRequested)
					{
						var data = _applyPayload
							? new ScaleMeasurementData(MeasurementStatus.Measured, 0.7)
							: new ScaleMeasurementData(MeasurementStatus.Ready, 0.0);

						MeasurementReady?.Invoke(data);
						await Task.Delay(1000);
					}
				}
				catch (Exception ex)
				{
					logger.LogException("Failed to send FakeScales data", ex);
				}}, _tokenSource.Token);
		}

		public void Dispose()
		{
			_tokenSource.Cancel();
		}

		public void ResetWeight()
		{
			_applyPayload = false;
		}

		private void OnstatusSwitchTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_applyPayload = !_applyPayload;
		}
	}
}