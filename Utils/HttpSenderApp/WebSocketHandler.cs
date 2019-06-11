using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebSocketHandler
{
	internal class WebSocketHandler : IDisposable
	{
		private readonly string _address;
		private readonly WebSocketServer _server;
		private readonly List<IWebSocketConnection> _activeConnections;

		private string _lastBarcode;
		private string _lastRank;
		private string _lastComment;
		private int _lastLength;
		private int _lastWidth;
		private int _lastHeight;
		private double _lastWeight;
		private int _lastStatus;

		private readonly Random _rnd;

		public WebSocketHandler(string ip, int port)
		{
			_address = $"ws://{ip}:{port}";

			_lastBarcode = "";
			_lastRank = "";
			_lastComment = "";
			_lastLength = 0;
			_lastWidth = 0;
			_lastHeight = 0;
			_lastWeight = 0.0;
			_lastStatus = 0;

			_activeConnections = new List<IWebSocketConnection>();

			Console.WriteLine($"Starting webSocket server for {_address}...");

			_server = new WebSocketServer(_address)
			{
				ListenerSocket = {NoDelay = true},
				RestartAfterListenError = true
			};

			_server.Start(socket =>
			{
				socket.OnOpen = () => { OnClientConnected(socket); };
				socket.OnClose = () => { OnClientDisconnected(socket); };
				socket.OnMessage = message =>
				{
					OnMessageReceived(socket, message);
				};
			});

			_rnd = new Random();
		}

		public void Dispose()
		{
			Console.WriteLine($"Stopping webSocket server for {_address}..");
			_server.Dispose();
		}

		private void OnMessageReceived(IWebSocketConnection socket, string message)
		{
			Console.WriteLine($"Message received from {socket.ConnectionInfo.ClientIpAddress}: {message}");

			var statusMessage = CreateOutgoingStatusMessage();

			var messageObject = (JObject) JsonConvert.DeserializeObject(message);
			var command = messageObject["command"].ToString();

			switch (command)
			{
				case "status":
					Console.WriteLine("Status command received");
					var statusToken = messageObject["status"];
					ParseStatusMessage(statusToken);
					break;
				case "start":
					Console.WriteLine("Start command received");
					ParseStartMessage();
					break;
				default:
					Console.WriteLine($"Unknown command received: {command}");
					break;
			}

			Broadcast(statusMessage);
		}

		private void ParseStartMessage()
		{
			_lastWeight = 0;
			_lastLength = 0;
			_lastWidth = 0;
			_lastHeight = 0;
			_lastStatus = 1;
			var outgoingMessage1 = CreateOutgoingStatusMessage();
			Broadcast(outgoingMessage1);

			Thread.Sleep(3000);
			_lastWeight = _rnd.NextDouble() * 100;
			_lastLength = _rnd.Next(1, 100);
			_lastWidth = _rnd.Next(1, 100);
			_lastHeight = _rnd.Next(1, 100);
			_lastStatus = 0;
			var outgoingMessage2 = CreateOutgoingStatusMessage();
			Broadcast(outgoingMessage2);
		}

		private void ParseStatusMessage(JToken statusObject)
		{
			var barcode = statusObject["barcode"].ToString();
			_lastBarcode = barcode;
			var rank = statusObject["rank"].ToString();
			_lastRank = rank;
			var comment = statusObject["comment"].ToString();
			_lastComment = comment;
			Console.WriteLine(
				$"status command received, barcode={barcode}, rank={rank}, comment={comment}");
			var outgoingMessage = CreateOutgoingStatusMessage();
			Broadcast(outgoingMessage);
		}

		private void Broadcast(string message)
		{
			_activeConnections.ToList().ForEach(s => s.Send(message));
		}

		private void OnClientConnected(IWebSocketConnection socket)
		{
			_activeConnections.Add(socket);
			var statusMessage = CreateOutgoingStatusMessage();
			Broadcast(statusMessage);
			Console.WriteLine($"Client connected {socket.ConnectionInfo.ClientIpAddress} client count={_activeConnections.Count}");
		}

		private void OnClientDisconnected(IWebSocketConnection socket)
		{
			_activeConnections.Remove(socket);
			Console.WriteLine($"Client disconnected {socket.ConnectionInfo.ClientIpAddress} client count={_activeConnections.Count}");
		}

		private string CreateOutgoingStatusMessage()
		{
			var commandDict = new Dictionary<string, object> { { "command", "status" } };
			var statusDict = new Dictionary<string, object>
			{
				{"status", _lastStatus},
				{"barcode", _lastBarcode},
				{"rank", _lastRank},
				{"comment", _lastComment},
				{"weight", _lastWeight},
				{"length", _lastLength},
				{"width", _lastWidth},
				{"height", _lastHeight}
			};
			commandDict.Add("status", statusDict);
			return JsonConvert.SerializeObject(commandDict);
		}
	}
}