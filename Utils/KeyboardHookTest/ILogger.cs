using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeyboardHookTest
{
	public interface ILogger
	{
		void LogInfo(string message);

		void LogError(string message);

		void LogException(string message, Exception ex);

		void LogDebug(string message);
	}
}
