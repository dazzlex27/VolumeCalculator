using System;
using System.IO;
using Primitives;

namespace VersionWriter
{
	internal class Program
	{
		private const string OutputFileName = "appversion.txt";

		private static void Main()
		{
			try
			{
				var outputFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				var outputFilePath = Path.Combine(outputFolder, OutputFileName);
				File.Delete(outputFilePath);
				using (var sw = File.AppendText(outputFilePath))
				{
					sw.WriteLine($"VCalc {GlobalConstants.AppVersion}");
				}

				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to extract version! {ex}");
				Environment.Exit(2);
			}
		}
	}
}