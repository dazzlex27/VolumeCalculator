using Primitives.Settings.Integration;
using Primitives.Settings;
using System;

namespace ExtIntegration.RequestSenders.SqlSenders
{
	internal static class SqlRequestEngineFactory
	{
		public static ISqlRequestSenderEngine CreateEngine(SqlRequestSettings settings, string command)
		{
			switch (settings.Provider)
			{
				case SqlProvider.MsSqlServer:
					return new MsSqlRequestEngine(settings, command);
				case SqlProvider.MySql:
					return new MySqlRequestEngine(settings, command);
			}

			throw new NotSupportedException($"Unsupported SQL provider requested");
		}
	}
}
