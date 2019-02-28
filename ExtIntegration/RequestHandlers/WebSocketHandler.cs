using System;
using Fleck;
using Primitives.Logging;
using Primitives.Settings;

namespace ExtIntegration.RequestHandlers
{
	public class WebClientHandler : IDisposable
	{
		private readonly ILogger _logger;
		private readonly string _address;
		private readonly WebSocketServer _server;

		public WebClientHandler(ILogger logger, WebSocketHandlerSettings settings)
		{
			_logger = logger;
			// "ws://0.0.0.0:8081"
			_address = $"ws://{settings.Address}:{settings.Port}";
			_server = new WebSocketServer(_address)
			{
				ListenerSocket = {NoDelay = true},
				RestartAfterListenError = true
			};

			_server.Start(socket =>
			{
				socket.OnOpen += OnOpen;
				socket.OnClose += OnClose;
				socket.OnMessage += OnMessage;
			});
		}

		public void Dispose()
		{
			_logger.LogInfo($"Disposing the websocket server at {_address}...");
			_server.Dispose();
		}

		private void BroadcastStatus()
		{

		}

		private void OnClose()
		{
			_logger.LogInfo($"Websocket client disconnected from {_address}...");
		}

		private void OnOpen()
		{
			_logger.LogInfo($"Websocket client connected to {_address}...");
			// TODO: broadcast current status
		}

		private void OnMessage(string obj)
		{
			// TODO
		}
	}
}