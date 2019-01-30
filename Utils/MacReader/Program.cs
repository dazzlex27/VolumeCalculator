using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace MacReader
{
	class Program
	{
		private const string OutputFileName = "output.txt";

		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("usage: MacReader.exe command" + 
				                  Environment.NewLine + "g - get address" + 
				                  Environment.NewLine + "c - check address for your machine");
				return;
			}

			var idBytes = GetAddr();

			switch (args[0])
			{
				case "g":
					var macLine = string.Join(" ", idBytes);
					File.WriteAllText(OutputFileName, macLine);
					Console.WriteLine($"Mac address is saved to {OutputFileName}");
					return;
				case "c":
					var ok = CheckIfOk(idBytes);
					var message = ok ? "not valid" : "valid";
					Console.WriteLine($"Mac address is {message}");
					return;
				default:
					Console.WriteLine($"Unknown command \"{args[0]}\", terminating...");
					return;
			}
		}

		public static byte[] GetAddr()
		{
			var serial = "";
			try
			{
				var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

				foreach (var o in searcher.Get())
				{
					var wmiHd = (ManagementObject) o;

					serial = wmiHd["SerialNumber"].ToString();
				}
			}
			catch (Exception ex)
			{
				Directory.CreateDirectory("c:/temp");
				using (var f = File.AppendText("c:/temp"))
				{
					f.WriteLine($"s1 f{ex}");
				}

				return new byte[0];
			}

			return string.IsNullOrEmpty(serial) ? new byte[] { 0 } : Encoding.ASCII.GetBytes(serial);
		}

		public static bool CheckIfOk(byte[] message)
		{
			try
			{
				var addr = GetF2();
				var str = Encoding.ASCII.GetBytes(addr);

				var isEqual = str.SequenceEqual(message);
				return !(str.Length > 10 && isEqual);
			}
			catch (Exception ex)
			{
				Directory.CreateDirectory("c:/temp");
				using (var f = File.AppendText("c:/temp"))
				{
					f.WriteLine($"s2 f{ex}");
				}

				return true;
			}
		}

		public static string GetF2()
		{
			var twoM = GetM();
			var twoL = GetL();

			return Encoding.ASCII.GetString(_fourF.Concat(twoM).Concat(twoL).ToArray());
		}

		private static byte[] _fourF = { 84, 111, 32, 98, 101, 32, 102, 105 };

		public static byte[] GetM()
		{
			return new byte[] { 108, 108, 101, 100, 32, 98, 121, 32 };
		}

		public static byte[] GetL()
		{
			return new byte[] { 79, 46, 69, 46, 77, 46 };
		}
	}
}
