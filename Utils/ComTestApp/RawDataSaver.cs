using System;
using System.IO;
using System.IO.Ports;
using GodSharp.SerialPort;

namespace ComTestApp
{
	internal class RawDataSaver
	{
		public RawDataSaver(string port)
		{
			var serialPort = new GodSerialPort(port, 9600, Parity.Even, 8, StopBits.One);
			serialPort.UseDataReceived(true, (sp, bytes) =>
			{
				ReadMessage(bytes);
			});

			serialPort.Open();
		}
		
		public void ReadMessage(byte[] messageBytes)
		{
			using (var file = File.AppendText("output.txt"))
			{
				var bytesString = string.Join(" ", messageBytes);
				//var string = 
				file.WriteLine(bytesString);
				Console.WriteLine(bytesString);
			}
		}
	}
}