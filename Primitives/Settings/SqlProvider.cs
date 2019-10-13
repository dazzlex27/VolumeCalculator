using System.ComponentModel;

namespace Primitives.Settings
{
	public enum SqlProvider
	{
		[Description("MS SQL Server")]
		MsSqlServer,
		[Description("MySQL")]
		MySql
	}
}