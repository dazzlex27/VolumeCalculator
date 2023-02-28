using System;

namespace Primitives.Logging
{
	public interface ILogger : IDisposable
	{
		void LogInfo(string message);

		void LogError(string message);

		void LogException(string message, Exception ex);

		void LogDebug(string message);
	}
}