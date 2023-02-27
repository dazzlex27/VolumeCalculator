using System;
using System.Threading;
using System.Threading.Tasks;

namespace KeyboardHookTest
{
	internal class InterceptKeys
	{
		public static void Main()
		{
			Task.Run(() =>
			{
				var interceptKeys = new InterceptKeys();
				interceptKeys.Run();
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
				var scanner = new GenericKeyboardBarcodeScanner(logger);
				scanner.CharSequenceFormed += Scanner_CharSequenceFormed;
			});
			while (true)
			{
				Thread.Sleep(100);
			}
		}

		private void Scanner_CharSequenceFormed(string obj)
		{
			Console.WriteLine(obj);
		}
	}
}
