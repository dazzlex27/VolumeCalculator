namespace Primitives
{
	public class IoEntry
	{
		public string Name { get; set; }
		public string Port { get; set; }

		public IoEntry(string name, string port)
		{
			Name = name;
			Port = port;
		}
	}
}