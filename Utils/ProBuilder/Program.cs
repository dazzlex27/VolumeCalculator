using System;
using System.IO;

namespace ProBuilder
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			try
			{
				if (args.Length < 1)
					Environment.Exit(1);

				switch(args[0])
				{
					case "s":
						Replace("false", "true");
						break;
					case "r":
						Replace("true", "false");
						break;
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

		private static void Replace(string from, string to)
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
				
				Environment.Exit(0);
			}
		}
	}
}