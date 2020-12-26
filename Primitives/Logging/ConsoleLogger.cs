using System;

namespace Primitives.Logging
{
	public class ConsoleLogger : ILogger
	{
		public void LogDebug(string message)
		{
			Console.WriteLine($"DEBUG: {message}");
		}

		public void LogError(string message)
		{
			Console.WriteLine($"ERROR: {message}");
		}

		public void LogException(string message, Exception ex)
		{
			Console.WriteLine($"EXCEPTION: {message}");
		}

		public void LogInfo(string message)
		{
			Console.WriteLine($"INFO: {message}");
		}
	}
}
