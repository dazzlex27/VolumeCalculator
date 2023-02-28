using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using GodSharp.SerialPort;
using Primitives.Logging;
using Timer = System.Timers.Timer;

namespace DeviceIntegration.Scales
{
	internal class OkaScales : IScales
	{
		public event Action<ScaleMeasurementData> MeasurementReady;

		private readonly ILogger _logger;
		private readonly string _port;
        private readonly int _minWeight;

		private readonly GodSerialPort _serialPort;
		private readonly CancellationTokenSource _tokenSource;

		private readonly Timer _requestTimer;

		private volatile bool _readFinished;

		private bool _paused;

		public OkaScales(ILogger logger, string port, int minWeight)
		{
			_logger = logger;
			_port = port;
            _minWeight = minWeight;

			_tokenSource = new CancellationTokenSource();

			_logger.LogInfo($"Creating OkaScales scales on port {port}...");

			_requestTimer = new Timer() { AutoReset = false, Interval = 3000 };
			_requestTimer.Elapsed += OnRequestTimerElapsed;

			_serialPort = new GodSerialPort(port, 4800, Parity.Even, 8, StopBits.Two);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				try
				{
					ReadMessage(bytes);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to read OkaScales message", ex);
				}
			});

			_serialPort.Open();

			var data = new byte[32];

			Task.Run(async () =>
			{
				try
				{
					await PollScales();
				}
				catch (Exception ex)
				{
					_logger.LogException("Exception in OkaScales polling loop", ex);
				}
			});
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing OkaScales scales on port {_port}...");
			_serialPort.Close();
		}

		public void ResetWeight()
		{
		}

		public void TogglePause(bool pause)
		{
			_paused = pause;
		}

		private void ReadMessage(byte[] messageBytes)
		{
			if (_paused)
				return;

			if (_readFinished)
				return;

			try
			{
				if (messageBytes== null || messageBytes.Length == 0)
					return;

				var weight = GetWeightFromMessage(messageBytes);
				var status = MeasurementStatus.Measured;
				if (weight < _minWeight)
				{
					status = MeasurementStatus.Ready;
					weight = 0;
				}

				var measurementData = new ScaleMeasurementData(status, weight);
				MeasurementReady?.Invoke(measurementData);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to read data from OkaScales", ex);
			}
			finally
			{
				_readFinished = true;
			}
		}

		private int GetWeightFromMessage(byte[] messageBytes)
		{
			try
			{
				var messageCopy = new byte[messageBytes.Length];

				Buffer.BlockCopy(messageBytes, 0, messageCopy, 0, messageBytes.Length);
				Array.Reverse(messageCopy);

				var joinedReversedWeightString = string.Join("", messageCopy);
				var trimmedString = joinedReversedWeightString.TrimStart(new char[] { '0' });
				if (trimmedString.Length == 0)
					trimmedString = "0";

				return int.Parse(trimmedString);
			}
			catch (Exception ex)
			{
				var messageBytesString = string.Join(" ", messageBytes);
				_logger.LogException($"Failed to parse OkaScalesMessage: {messageBytesString}", ex);

				return -1;
			}
		}

		private async Task PollScales()
		{
			const int errorTimeOutMs = 500;

			var pollWeightMessageArray = new byte[32];
			pollWeightMessageArray[31] = 3;

			_readFinished = true;

			while (!_tokenSource.IsCancellationRequested)
			{
				try
				{
					if (!_readFinished)
					{
						if (!_requestTimer.Enabled)
							_requestTimer.Start();

						await Task.Delay(50);
						continue;
					}

					_requestTimer.Stop();

					_readFinished = false;

					_serialPort.Write(pollWeightMessageArray);

					await Task.Delay(500);
				}
				catch (Exception ex)
				{
					_logger.LogException("Failed to poll data from OkaScales", ex);
					await Task.Delay(errorTimeOutMs);
				}
			}
		}

		private void OnRequestTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_readFinished = true;
		}
	}
}