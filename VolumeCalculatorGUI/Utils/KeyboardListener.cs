using System;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Common;

namespace VolumeCalculatorGUI.Utils
{
	internal class KeyboardListener
	{
		private const int TimerIntervalMs = 100;
		private const int TimeOutTimerIntervalMs = 500;

		public event Action<string> CharSequenceFormed;

		private readonly KeyToCharMapper _keyToCharMapper;

		private readonly Timer _keyCollectionTimer;
		private readonly Timer _timeOutTimer;

		private bool _onTimeOut;

		private StringBuilder _keyCollectionBuilder;

		public KeyboardListener(ILogger logger)
		{
			logger.LogInfo("Creating a keyboard listener...");

			_keyToCharMapper = new KeyToCharMapper();

			_keyCollectionTimer = new Timer(TimerIntervalMs) {AutoReset = false};
			_keyCollectionTimer.Elapsed += KeyCollectionTimer_Elapsed;

			_timeOutTimer = new Timer(TimeOutTimerIntervalMs) {AutoReset = false};
			_timeOutTimer.Elapsed += TimeOutTimer_Elapsed;

			_onTimeOut = false;

			EventManager.RegisterClassHandler(typeof(Window),
				Keyboard.KeyUpEvent, new KeyEventHandler(AddKey), true);
		}

		private void AddKey(object sender, KeyEventArgs e)
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