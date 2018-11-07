using System;

namespace Primitives
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