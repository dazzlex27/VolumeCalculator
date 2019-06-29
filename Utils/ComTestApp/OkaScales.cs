using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using GodSharp.SerialPort;
using Timer = System.Timers.Timer;

namespace ComTestApp
{
	internal class OkaScales
	{
		private readonly string _port;
		private readonly GodSerialPort _serialPort;
		private readonly CancellationTokenSource _tokenSource;

		private readonly Timer _requestTimer;

		private volatile bool _readFinished;

		public OkaScales(string port)
		{
			_port = port;
			_tokenSource = new CancellationTokenSource();

			_requestTimer = new Timer() { AutoReset = false, Interval = 3000 };
			_requestTimer.Elapsed += OnRequestTimerElapsed;

			//_logger.LogInfo($"Creating OkaScales scales on port {port}...");

			_serialPort = new GodSerialPort(port, 4800, Parity.Even, 8, StopBits.Two);
			_serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				try
				{
					ReadMessage(bytes);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Failed to read OkaScales message: " + ex);
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
					Console.WriteLine("Exception in OkaScales polling loop: " + ex);
				}
			});
		}

		private void OnRequestTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_readFinished = true;
		}

		public void Dispose()
		{
			Console.WriteLine($"Disposing OkaScales scales on port {_port}...");
			_serialPort.Close();
		}

		public void ResetWeight()
		{
		}

		private void ReadMessage(byte[] messageBytes)
		{
			if (_readFinished)
				return;

			try
			{
				var messageConcat = string.Join(" ", messageBytes);
				//Console.WriteLine($"message: {messageConcat}");

				if (messageBytes.Length == 0)
					return;

				var weight = GetWeightFromMessage(messageBytes);
				var status = MeasurementStatus.Measured;
				if (weight < 1)
				{
					status = MeasurementStatus.Ready;
					weight = 0;
				}

				var measurementData = new ScaleMeasurementData(status, weight);
				Console.WriteLine($"result: {weight}");
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed to read data from OkaScales: " + ex);
			}
			finally
			{
				_readFinished = true;
			//	Console.WriteLine("read finished = true");
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
				Console.WriteLine($"Failed to parse OkaScalesMessage: {messageBytesString}: " +  ex);

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
					//	Console.WriteLine("waiting to finish reading...");
						continue;
					}

					_requestTimer.Stop();

					_readFinished = false;
				//	Console.WriteLine("read finished = false");

					_serialPort.Write(pollWeightMessageArray);

					await Task.Delay(500);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Failed to poll data from OkaScales: " + ex);
					await Task.Delay(errorTimeOutMs);
				}
			}
		}
	}
}