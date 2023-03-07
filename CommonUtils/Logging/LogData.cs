namespace CommonUtils.Logging
{
	internal struct LogData
	{
		public string Type;
		public string Message;

		public LogData(string type, string message)
		{
			Type = type;
			Message = message;
		}
	}
}
