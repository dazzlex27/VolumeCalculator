﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DeviceIntegration;
using DeviceIntegration.Scales;
using Primitives.Logging;

namespace ComTestApp
{
	internal class Program
	{
		private static void Main()
		{
			var logger = new ConsoleLogger();

			//TestOkaScales(logger);
			//TestLaser(logger);
			//TestCasM(logger);
			//SaveRawData(logger);
			//TestMassaK(logger);
			//TestCi2001A(logger);
			TestKeUsb24R(logger);
			Console.ReadKey();
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
			Task.Run(() =>
			{
				var saver = new RawDataSaver(Console.ReadLine());
			});
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

			return DeviceIntegrationCommon.CreateRequestedScales(id, logger, portToUse, minWeight);
		}

		private static void TestKeUsb24R(ILogger logger)
		{
			var ioCircuit = DeviceIntegrationCommon.CreateRequestedIoCircuit("keusb24r", logger, "COM5");
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