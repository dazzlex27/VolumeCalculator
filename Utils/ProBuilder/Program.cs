using System;
using System.IO;

namespace ProBuilder
{
	internal class Program
	{
		private static void Main()
		{
			try
			{
				var fileLines = File.ReadAllLines(@"..\..\Primitives\GlobalConstans.cs");

				for (var i = 0; i < fileLines.Length; i++)
				{
					if (!fileLines[i].Contains("Edition = "))
						continue;

					fileLines[i] = fileLines[i].Replace("false", "true");

					Console.WriteLine("Edition switched to Pro");
					Environment.Exit(0);
				}

				Console.WriteLine("Failed to find edition line!");
				Environment.Exit(2);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to change app edition! {ex}");
				Environment.Exit(1);
			}
		}
	}
}