using System;
using System.IO;

namespace ProBuilder
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			try
			{
				if (args.Length < 1)
					return 3;

				var exitCode = 1;

				switch(args[0])
				{
					case "s":
						exitCode = Replace("false", "true");
						break;
					case "r":
						exitCode = Replace("true", "false");
						break;
				}

				if (exitCode == 0)
					return 0;

				Console.WriteLine("Failed to find edition line!");
				return 2;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to change app edition! {ex}");
				return 1;
			}
		}

		private static int Replace(string from, string to)
		{
			var fileLines = File.ReadAllLines(@"..\..\Primitives\GlobalConstants.cs");

			for (var i = 0; i < fileLines.Length; i++)
			{
				if (!fileLines[i].Contains("Edition = "))
					continue;

				fileLines[i] = fileLines[i].Replace(from, to);

				switch (to)
				{
					case "true":
						Console.WriteLine("Edition switched to Pro");
						break;
					case "false":
						Console.WriteLine("Edition restored");
						break;
				}

				return 0;
			}

			return 1;
		}
	}
}