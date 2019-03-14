using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Primitives.Logging;
using Timer = System.Timers.Timer;

namespace DeviceIntegration.Scanners
{
	public class GenericKeyboardBarcodeScanner : IBarcodeScanner
	{
		public event Action<string> CharSequenceFormed;

		private const int TimerIntervalMs = 200;
		private const int TimeOutTimerIntervalMs = 300;

		private const int KeyboardHookKey = 13;
		private const int KeyDownEventKey = 0x0100;

		private static readonly List<char> NumericKeys = "1234567890".ToCharArray().ToList();
		private static readonly List<char> LetterKeys = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray().ToList();

		private static LowLevelKeyboardProc _proc; // Keep as class member, otherwise it gets GC'd!!!
		private static IntPtr _hookId;

		private readonly ILogger _logger;
		private readonly Timer _keyCollectionTimer;
		private readonly Timer _timeOutTimer;
		private readonly StringBuilder _keyCollectionBuilder;

		private bool _onTimeOut;

		public GenericKeyboardBarcodeScanner(ILogger logger)
		{
			_logger = logger;

			_logger.LogInfo("Starting a generic keyboard scanner...");

			_keyCollectionTimer = new Timer(TimerIntervalMs) { AutoReset = false };
			_keyCollectionTimer.Elapsed += KeyCollectionTimer_Elapsed;

			_timeOutTimer = new Timer(TimeOutTimerIntervalMs) { AutoReset = false };
			_timeOutTimer.Elapsed += TimeOutTimer_Elapsed;

			_keyCollectionBuilder = new StringBuilder();

			_onTimeOut = false;

			_proc = HookCallback;
			_hookId = SetHook(_proc);
		}

		public void Dispose()
		{
			_logger.LogInfo("Disposing a generic keyboard scanner...");
			UnhookWindowsHookEx(_hookId);
		}

		private void AddKey(Keys key)
		{
			if (_onTimeOut)
				return;

			if (_keyCollectionBuilder.Length == 0)
				_keyCollectionTimer.Start();

			var keyString = key.ToString().Replace("D", "");
			if (keyString.Length != 1)
				return;

			var keyChar = keyString[0];
			if (NumericKeys.Contains(keyChar) || LetterKeys.Contains(keyChar))
				_keyCollectionBuilder.Append(keyChar);
		}

		private void TimeOutTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_keyCollectionBuilder.Clear();
			_onTimeOut = false;
		}

		private void KeyCollectionTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			_onTimeOut = true;
			_timeOutTimer.Start();
			CharSequenceFormed?.Invoke(_keyCollectionBuilder.ToString());
		}

		private static IntPtr SetHook(LowLevelKeyboardProc proc)
		{
			using (var curProcess = Process.GetCurrentProcess())
			using (var curModule = curProcess.MainModule)
			{
				return SetWindowsHookEx(KeyboardHookKey, proc, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode < 0 || wParam != (IntPtr)KeyDownEventKey)
				return CallNextHookEx(_hookId, nCode, wParam, lParam);

			var vkCode = Marshal.ReadInt32(lParam);

			AddKey((Keys)vkCode);

			return CallNextHookEx(_hookId, nCode, wParam, lParam);
		}

		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}