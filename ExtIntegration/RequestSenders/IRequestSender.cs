using System;
using Primitives;

namespace ExtIntegration
{
	public interface IRequestSender : IDisposable
	{
		void Connect();

		bool Send(CalculationResult result);

		void Disconnect();
	}
}