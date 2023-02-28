using System;
using System.Threading.Tasks;
using Primitives;

namespace ExtIntegration.RequestSenders
{
	public interface IRequestSender : IDisposable
	{
		Task ConnectAsync();

		Task<bool> SendAsync(CalculationResultData resultData);
	}
}
