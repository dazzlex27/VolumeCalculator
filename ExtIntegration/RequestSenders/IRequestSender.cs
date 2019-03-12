using System;
using Primitives;

namespace ExtIntegration.RequestSenders
{
	public interface IRequestSender : IDisposable
	{
		void Connect();

		bool Send(CalculationResultData resultData);
	}
}