using System;

namespace Primitives.Logging
{
	public sealed class DummyLogger : ILogger
	{
		public void LogDebug(string _)
		{
		}

		public void LogError(string _)
		{
		}

		public void LogException(string _, Exception __)
		{
		}

		public void LogInfo(string _)
		{
		}

		public void Dispose()
		{
		}
	}
}
