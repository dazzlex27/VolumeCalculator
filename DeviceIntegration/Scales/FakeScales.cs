﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Primitives.Logging;

namespace DeviceIntegration.Scales
{
	internal class FakeScales : IScales
	{
		private static readonly ConcurrentQueue<int> Values = new ConcurrentQueue<int>(new[] {135, 407});
		
		private readonly CancellationTokenSource _tokenSource;

		public event Action<ScaleMeasurementData> MeasurementReady;

		private volatile bool _applyPayload;

		private bool _paused;

		public FakeScales(ILogger logger)
		{
			_tokenSource = new CancellationTokenSource();
			_applyPayload = true;
			Values.TryDequeue(out var value);

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
								? new ScaleMeasurementData(MeasurementStatus.Measured, value)
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
			_applyPayload = !_applyPayload;
		}
	}
}