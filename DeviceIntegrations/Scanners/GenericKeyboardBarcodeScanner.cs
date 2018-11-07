using System;
using System.Text;
using System.Timers;
using System.Windows.Input;
using Primitives;

namespace DeviceIntegrations.Scanners
{
	public class GenericKeyboardBarcodeScanner : IBarcodeScanner
	{
		private const int TimerIntervalMs = 200;
		private const int TimeOutTimerIntervalMs = 300;

		public event Action<string> CharSequenceFormed;

		private readonly ILogger _logger;

		private readonly KeyToCharMapper _keyToCharMapper;

		private readonly Timer _keyCollectionTimer;
		private readonly Timer _timeOutTimer;

		private bool _onTimeOut;

		private StringBuilder _keyCollectionBuilder;

		public GenericKeyboardBarcodeScanner(ILogger logger)
		{
			_logger = logger;

			_logger.LogInfo("Starting a generic keyboard scanner...");

			_keyToCharMapper = new KeyToCharMapper();

			_keyCollectionTimer = new Timer(TimerIntervalMs) {AutoReset = false};
			_keyCollectionTimer.Elapsed += KeyCollectionTimer_Elapsed;

			_timeOutTimer = new Timer(TimeOutTimerIntervalMs) {AutoReset = false};
			_timeOutTimer.Elapsed += TimeOutTimer_Elapsed;

			_onTimeOut = false;
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing a generic keyboard scanner...");
		}

		public void AddKey(object sender, KeyEventArgs e)
		{
			if (_onTimeOut)
				return;

			if (_keyCollectionBuilder == null)
			{
				_keyCollectionBuilder = new StringBuilder();
				_keyCollectionTimer.Start();
			}

			var character = _keyToCharMapper.GetCharFromKey(e.Key);
			_keyCollectionBuilder.Append(character);
		}

		private void TimeOutTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_keyCollectionBuilder = null;
			_onTimeOut = false;
		}

		private void KeyCollectionTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_onTimeOut = true;
			_timeOutTimer.Start();
			CharSequenceFormed?.Invoke(_keyCollectionBuilder.ToString());
		}
	}
}