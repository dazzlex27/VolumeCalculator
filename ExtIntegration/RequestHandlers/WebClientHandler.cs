using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Primitives;
using Primitives.Logging;
using Primitives.Settings.Integration;

namespace ExtIntegration.RequestHandlers
{
	public class WebClientHandler : IDisposable
	{
		public event Action<CalculationRequestData> CalculationStartRequested;

		private readonly ILogger _logger;
		private readonly string _address;
		private readonly WebSocketServer _server;
		private readonly List<IWebSocketConnection> _activeConnections;

		private int _lastStatus;
		private CalculationRequestData _lastRequestData;
		private CalculationResult _lastCalculationResult;

		public WebClientHandler(ILogger logger, WebClientHandlerSettings settings)
		{
			_logger = logger;

			_activeConnections = new List<IWebSocketConnection>();
			_lastRequestData = new CalculationRequestData("", 0, "");
			_lastCalculationResult =
				new CalculationResult(DateTime.Now, "", 0, WeightUnits.Gr, 
					0, 0, 0, 0, 0, 
					"", false);


			// "ws://0.0.0.0:8081"
			_address = $"ws://{settings.Address}:{settings.Port}";

			_logger.LogInfo($"Starting a web client handler... ws - {_address}");

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
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing the websocket server at {_address}...");
			_server.Dispose();
		}

		public void UpdateCalculationStatus(CalculationStatus status)
		{
			var statusInt = 0;
			switch (status)
			{
				case CalculationStatus.Running:
					statusInt = 1;
					break;
				case CalculationStatus.Successful:
					statusInt = 0;
					break;
				default:
					statusInt = 2;
					break;
			}

			_lastStatus = statusInt;
			BroadcastStatusMessage();
		}

		public void UpdateCalculationRequestData(CalculationRequestData data)
		{
			_lastRequestData = data;
			BroadcastStatusMessage();
		}

		public void UpdateCalculationResult(CalculationResultData resultData)
		{
			_lastCalculationResult = resultData.Result;
			BroadcastStatusMessage();
		}

		private void OnMessageReceived(IWebSocketConnection socket, string message)
		{
			try
			{
				_logger.LogInfo($"Message received from {socket.ConnectionInfo.ClientIpAddress}: {message}");

				var messageObject = (JObject) JsonConvert.DeserializeObject(message);
				var command = messageObject["command"].ToString();

				switch (command)
				{
					case "status":
						_logger.LogInfo("Status command received");
						var statusToken = messageObject["status"];
						ParseStatusMessage(statusToken, socket);
						break;
					case "start":
						_logger.LogInfo("Start command received");
						ParseStartMessage();
						break;
					default:
						_logger.LogInfo($"Unknown command received: {command}");
						break;
				}

				BroadcastStatusMessage(socket);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to parse status message", ex);
			}
		}

		private void ParseStartMessage()
		{
			CalculationStartRequested?.Invoke(_lastRequestData);
		}

		private void ParseStatusMessage(JToken statusObject, IWebSocketConnection excludeSocket)
		{
			try
			{
				var barcode = statusObject["barcode"].ToString();
				uint.TryParse(statusObject["rank"].ToString(), out var rank);
				var comment = statusObject["comment"].ToString();
				_logger.LogInfo($"status command received, barcode={barcode}, rank={rank}, comment={comment}");
				_lastRequestData = new CalculationRequestData(barcode, rank, comment);
				BroadcastStatusMessage(excludeSocket);
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to parse status message", ex);
			}
		}

		private void OnClientConnected(IWebSocketConnection socket)
		{
			_activeConnections.Add(socket);
			BroadcastStatusMessage();
			_logger.LogInfo($"Web client connected {socket.ConnectionInfo.ClientIpAddress} client count={_activeConnections.Count}");
		}

		private void OnClientDisconnected(IWebSocketConnection socket)
		{
			_activeConnections.Remove(socket);
			_logger.LogInfo($"Web client disconnected {socket.ConnectionInfo.ClientIpAddress} client count={_activeConnections.Count}");
		}

		private void BroadcastStatusMessage(IWebSocketConnection excludeSocket = null)
		{
			try
			{
				if (_activeConnections.Count == 0)
					return;

				var message = CreateStatusMessage();

				var allSocketsButTheExcludedOne = excludeSocket == null
					? _activeConnections
					: _activeConnections.ToList().Where(s => s != excludeSocket).ToList();
				allSocketsButTheExcludedOne.ForEach(s => s.Send(message));
			}
			catch (Exception ex)
			{
				_logger.LogException("Failed to broadcast status message", ex);
			}
		}

		private string CreateStatusMessage()
		{
			var commandDict = new Dictionary<string, object> { { "command", "status" } };
			var statusDict = new Dictionary<string, object>
			{
				{"status", _lastStatus},
				{"barcode", _lastCalculationResult.Barcode},
				{"rank", _lastRequestData.UnitCount},
				{"comment", _lastRequestData.Comment},
				{"weight", _lastCalculationResult.ObjectWeight},
				{"length", _lastCalculationResult.ObjectLengthMm},
				{"width", _lastCalculationResult.ObjectWidthMm},
				{"height", _lastCalculationResult.ObjectHeightMm}
			};
			commandDict.Add("status", statusDict);
			return JsonConvert.SerializeObject(commandDict);
		}
	}
}