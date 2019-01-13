namespace ExtIntegration
{
	public class RequestParameter
	{
		public string Name { get; }

		public string Value { get; }

		public RequestParameter(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}
}