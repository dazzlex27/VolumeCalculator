using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonUtils.Logging;
using DeviceIntegration;
using DeviceIntegration.Scales;
using Primitives.Logging;

namespace ComTestApp
{
	internal class Program
	{
		private static void Main()
		{
			// TODO: turn this into proper device tests
			var logger = new ConsoleLogger();

			//TestOkaScales(logger);
			//TestLaser(logger);
			//TestCasM(logger);
			//SaveRawData(logger);
			//TestMassaK(logger);
			//TestCi2001A(logger);
			//TestKeUsb24R(logger);
			var byteArray = new byte[] {1, 2, 83, 32, 48, 46, 48, 55, 51, 55, 107, 103, 98, 3, 4};
			var weight = TestWeightCheck(byteArray);
			Console.ReadKey();
		}
		
		private static int TestWeightCheck(IReadOnlyCollection<byte> messageBytes)
		{
			if (messageBytes == null || messageBytes.Count < 12)
				throw new ArgumentException("MasC scales: message is too short");

			var weightArray = messageBytes.Skip(4).Take(6).ToArray();
			var weightString = Encoding.ASCII.GetString(weightArray);
			var weight = double.Parse(weightString, CultureInfo.InvariantCulture);

			var unitsArray = messageBytes.Skip(10).Take(2).ToArray();
			var unitsString = Encoding.ASCII.GetString(unitsArray);

			double multiplier;

			switch (unitsString)
			{
				case "kg":
					multiplier = 1000;
					break;
				case "lb":
					multiplier = 453.59237;
					break;
				case " g":
					multiplier = 1;
					break;
				case "oz":
					multiplier = 28.3495;
					break;
				default:
					//_logger.LogError($"CasM scales: failed to find weight multiplier \"{unitsString}\"");
					multiplier = 0;
					break;
			}

			return (int)Math.Floor(weight * multiplier);
		}

		private static void TestOkaScales(ILogger logger)
		{
			var scales = CreateScales("oka", logger);
		}

		//private static void TestLaser(ILogger logger)
		//{
		//	var rangefinder = DeviceIntegrationCommon.CreateRequestedRangeMeter("custom", logger);
		//	var open = rangefinder.openDevice();
		//	if (!open)
		//	{
		//		Console.WriteLine("failed to open");
		//		return;
		//	}
		//	rangefinder.turnOnLaser();
		//	Thread.Sleep(200);
		//	rangefinder.readButton();
		//	Thread.Sleep(500);
		//	rangefinder.readCurrentRecord();
		//	var rangeM = rangefinder.lastDistance / 10000.0;
		//	Console.WriteLine($"{rangeM}m");
		//}

		private static void SaveRawData()
		{
			Task.Factory.StartNew((o) =>
			{
				var saver = new RawDataSaver(Console.ReadLine());
			}, TaskCreationOptions.LongRunning);
			while (true)
			{
				Thread.Sleep(100);
			}
		}

		private static void TestCi2001A(ILogger logger)
		{
			var scales = CreateScales("ci2001a", logger);
			scales.MeasurementReady += data => { Console.WriteLine($"{data.Status} {data.WeightGr}Gr"); };
			while (true)
			{
				Thread.Sleep(1000);
			}
		}

		private static void TestCasM(ILogger logger)
		{
			var scales = CreateScales("casm", logger);
			scales.MeasurementReady += data => { Console.WriteLine($"{data.Status} {data.WeightGr}Gr"); };
			while (true)
			{
				Thread.Sleep(1000);
			}
		}

		private static void TestMassaK(ILogger logger)
		{
			var scales = CreateScales("massak", logger, 1000);
			Thread.Sleep(1000);
			scales.ResetWeight();
			Thread.Sleep(1000);
			scales.Dispose();
		}

		private static IScales CreateScales(string id, ILogger logger, int minWeight = 0, string port = "")
		{
			var portToUse = port;

			if (port == "")
			{
				Console.WriteLine($"Enter port to use for {id} (COM1, etc)");
				portToUse = Console.ReadLine();
			}

			return DeviceFactory.CreateRequestedScales(id, logger, portToUse, minWeight);
		}

		private static void TestKeUsb24R(ILogger logger)
		{
			var ioCircuit = DeviceFactory.CreateRequestedIoCircuit("keusb24r", logger, "COM5");
			ioCircuit.WriteData(",AFR,0");
			while (true)
			{
				try
				{
					var dataArray = Console.ReadLine().Split(' ');
					switch (dataArray[0])
					{
						case "r":
						{
							var num = int.Parse(dataArray[1]);
							var state = dataArray[2];
							ioCircuit.ToggleRelay(num, state == "1");
							break;
						}
						case "l":
						{
							var stopWatch = new Stopwatch();
							stopWatch.Start();
							var num = int.Parse(dataArray[1]);
							var value = ioCircuit.PollLine(num);
							stopWatch.Stop();
							logger.LogInfo($"LINE {num}: {value}, elapsed={stopWatch.ElapsedMilliseconds}");
							break;
						}
						default:
							logger.LogError("unknown command received");
							break;
					}
				}
				catch (Exception ex)
				{
					logger.LogException("failed to parse input data", ex);
				}
			}
		}
	}
}
