using System;

namespace Primitives.Logging
{
	public class DummyLogger : ILogger
	{
		public void LogInfo(string info)
		{
		}

		public void LogError(string info)
		{
		}

		public void LogException(string info, Exception ex)
		{
		}
	}
}