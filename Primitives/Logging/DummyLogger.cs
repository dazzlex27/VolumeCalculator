using System;

namespace Primitives.Logging
{
	public class DummyLogger : ILogger
	{
		public void LogInfo(string message)
		{
		}

		public void LogError(string message)
		{
		}

		public void LogException(string message, Exception ex)
		{
		}

		public void LogDebug(string message)
		{
		}
	}
}