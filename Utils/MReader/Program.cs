using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace MReader
{
	internal class Program
	{
		private const string OutputFileName = "output.txt";

		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("usage: MReader.exe command" +
								  Environment.NewLine + "g - get data" +
								  Environment.NewLine + "c - check data");
				return;
			}

			var idBytes = GetAddr();

			switch (args[0])
			{
				case "g":
					var macLine = string.Join(" ", idBytes);
					File.WriteAllText(OutputFileName, macLine);
					Console.WriteLine($"Data is saved to {OutputFileName}");
					return;
				case "c":
					var ok = CheckIfOk(idBytes);
					var message = ok ? "Not valid" : "Valid";
					Console.WriteLine($"{message}");
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
				var messageString = string.Join(" ", message);
				var messageBytes = Encoding.ASCII.GetBytes(messageString);

				var addr = GetF2();
				var str = Encoding.ASCII.GetBytes(addr);

				var isEqual = str.SequenceEqual(messageBytes);
				return !(str.Length > 10 && isEqual);
			}
			catch (Exception ex)
			{
				try
				{
					Console.WriteLine("File not found");

					Directory.CreateDirectory("c:/temp");
					using (var f = File.AppendText("c:/temp"))
					{
						f.WriteLine($"s2 f{ex}");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("failed to write data");
				}


				return true;
			}
		}

		public static string GetF2()
		{
			var l2 = GetL();
			var bytesString = Encoding.ASCII.GetString(l2);
			return File.ReadAllText(bytesString);
		}

		public static byte[] GetL()
		{
			// Path to license file: C:/Program Files/MOXA/USBDriver/v2.txt
			return new byte[] { 67, 58, 47, 80, 114, 111, 103, 114, 97, 109, 32, 70, 105, 108, 101, 115, 47, 77, 79,
				88, 65, 47, 85, 83, 66, 68, 114, 105, 118, 101, 114, 47, 118, 50, 46, 116, 120, 116 };
		}
	}
}
