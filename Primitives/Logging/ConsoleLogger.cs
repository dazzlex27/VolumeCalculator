using System;

namespace Primitives.Logging
{
	public sealed class ConsoleLogger : ILogger
	{
		public void LogDebug(string message)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine($"DEBUG: {message}");
			Console.ResetColor();
		}

		public void LogError(string message)
		{
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine($"ERROR: {message}");
			Console.ResetColor();
		}

		public void LogException(string message, Exception ex)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"EXCEPTION: {message}");
			Console.ResetColor();
		}

		public void LogInfo(string message)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"INFO: {message}");
			Console.ResetColor();
		}

		public void Dispose()
		{
		}
	}
}
