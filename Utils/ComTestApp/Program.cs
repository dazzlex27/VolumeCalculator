using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComTestApp
{
	internal class Program
	{
		private static void Main()
		{
			//TestOkaScales();
			//TestLaser();
			//TestCasM();
			//SaveRawData();
			TestMassaK();
			//TestCi2001A();
			Console.ReadKey();
		}

		private static void TestOkaScales()
		{
			Console.WriteLine("Port:");
			var port = Console.ReadLine();
			var scales = new OkaScales(port);
		}

		private static void TestLaser()
		{
			//var rangefinder = new TeslaM70RangeMeter();
			//var open = rangefinder.openDevice();
			//if (!open)
			//{
			//	Console.WriteLine("failed to open");
			//	return;
			//}
			//rangefinder.turnOnLaser();
			//Thread.Sleep(200);
			//rangefinder.readButton();
			//Thread.Sleep(500);
			//rangefinder.readCurrentRecord();
			//var rangeM = rangefinder.lastDistance / 10000.0;
			//Console.WriteLine($"{rangeM}m");
		}

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

		private static void TestCi2001A()
		{
			var scales = new Ci2001AScales(Console.ReadLine());
			scales.MeasurementReady += data => { Console.WriteLine($"{data.Status} {data.WeightKg}kg"); };
			while (true)
			{
				Thread.Sleep(1000);
			}
		}
		//private static void TestCasM()
		//{
		//	// 1 2 83 32 48 46 48 48 49 50 107 103 98 3 4 0
		//	var messageBytes = new byte[] { 1, 2, 83, 32, 48, 46, 48, 48, 49, 50, 107, 103, 98, 3, 4, 0 };
		//	//var messageBytes = new byte[] { 1, 2, 85, 32, 48, 46, 48, 48, 48, 48, 107, 103, 103, 3, 4, 16 }; // measuring 0kg
		//	var casM = new RawDataSaver();
		//	casM.MeasurementReady += (data) => { Console.WriteLine($"{data.Status} {data.WeightKg}kg"); };
		//	casM.ReadMessage(messageBytes);
		//}

		private static void TestMassaK()
		{
			var port = Console.ReadLine();
			var scales = new MassaKScales(port, 1000);
			Thread.Sleep(1000);
			scales.ResetWeight();
			Thread.Sleep(1000);
			scales.Dispose();
		}
	}
}