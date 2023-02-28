using Primitives;
using System;
using System.Threading.Tasks;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	internal interface ISqlRequestSenderEngine : IDisposable
	{
		Task ConnectAsync();

		Task DisconnectAsync();

		Task<int> SendAsync(CalculationResultData resultData);

		string GetConnectionString();
	}
}
