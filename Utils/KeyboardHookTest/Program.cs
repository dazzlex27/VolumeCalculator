using BarcodeScanners;
using Primitives.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyboardHookTest
{
	internal class Program
	{
		public static void Main()
		{
			Task.Run(() =>
			{
				var program = new Program();
				program.Run();
			});
			while (true)
			{
				Thread.Sleep(1000);
			}
		}

		private void Run()
		{
			Task.Run(() =>
			{
				var logger = new DummyLogger();
				var scanner = new GenericKeyboardBarcodeScanner(logger, "");
				scanner.CharSequenceFormed += OnScannerCharSequenceFormed;
			});
			while (true)
			{
				Thread.Sleep(100);
			}
		}

		private void OnScannerCharSequenceFormed(string obj)
		{
			Console.WriteLine(obj);
		}
	}
}
