using System;

namespace Common
{
	public interface ILogger
	{
		void LogInfo(string info);

		void LogError(string info);

		void LogException(string info, Exception ex);
	}
}