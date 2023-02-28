using Primitives;
using System;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	internal interface ISqlRequestSenderEngine : IDisposable
	{
		void Connect();

		void Disconnect();

		int Send(CalculationResultData resultData);

		string GetConnectionString();
	}
}