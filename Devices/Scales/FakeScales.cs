using System;
using System.Threading;
using System.Threading.Tasks;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class FakeScales : IScales
	{ 
		public event Action<ScaleMeasurementData> MeasurementReady;

		private const int MaxWeight = 15000;
		private readonly Random _rand;
		private double _nextWeight;
		private double _minWeight;
		
		private readonly CancellationTokenSource _tokenSource;

		private volatile bool _applyPayload;
		private volatile bool _paused;

		public FakeScales(ILogger logger, string port, double minWeight)
		{
			_minWeight = minWeight;
			_rand = new Random();
			_tokenSource = new CancellationTokenSource();
			_applyPayload = true;
			_nextWeight = _rand.Next(MaxWeight);

			logger.LogInfo($"Creating FakeScales on 'port' {port}");

			Task.Run(async () => {
				try
				{
					var statusSwitchTimer = new System.Timers.Timer(10000) { AutoReset = true };
					statusSwitchTimer.Elapsed += OnstatusSwitchTimerElapsed;
					statusSwitchTimer.Start();

					while (!_tokenSource.IsCancellationRequested)
					{
						if (!_paused)
						{
							var data = _applyPayload
								? new ScaleMeasurementData(MeasurementStatus.Measured, _nextWeight)
								: new ScaleMeasurementData(MeasurementStatus.Ready, 0);

							MeasurementReady?.Invoke(data);
						}
						await Task.Delay(500);
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
			_tokenSource.Dispose();
		}

		public void ResetWeight()
		{
			_applyPayload = false;
		}

		public void TogglePause(bool pause)
		{
			_paused = pause;
		}

		private void OnstatusSwitchTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_nextWeight = _rand.Next(MaxWeight);
			_applyPayload = !_applyPayload;
		}
	}
}
