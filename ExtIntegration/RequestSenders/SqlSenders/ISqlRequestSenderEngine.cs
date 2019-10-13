using Primitives;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	internal interface ISqlRequestSenderEngine
	{
		void Connect();

		void Disconnect();

		int Send(CalculationResultData resultData);

		string GetConnectionString();
	}
}