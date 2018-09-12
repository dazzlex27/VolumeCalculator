using System;
using Common;

namespace VolumeCalculatorTest
{
	internal class DummyLogger : ILogger
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