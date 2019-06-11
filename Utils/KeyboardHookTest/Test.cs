//using System;
//using System.Diagnostics;
//using System.Windows.Forms;
//using System.Runtime.InteropServices;

//namespace KeyboardHookTest
//{
//	internal class InterceptKeys
//	{
//		private const int KeyboardHookKey = 13;
//		private const int KeyDownEventKey = 0x0100;

//		private static LowLevelKeyboardProc _proc;
//		private static IntPtr _hookId = IntPtr.Zero;

//		public static void Main()
//		{
//			_proc = HookCallback;

//			_hookId = SetHook(_proc);

//			Application.Run();

//			UnhookWindowsHookEx(_hookId);
//		}

//		private static IntPtr SetHook(LowLevelKeyboardProc proc)
//		{
//			using (var curProcess = Process.GetCurrentProcess())
//			using (var curModule = curProcess.MainModule)
//			{
//				return SetWindowsHookEx(KeyboardHookKey, proc, GetModuleHandle(curModule.ModuleName), 0);
//			}
//		}

//		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

//		private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
//		{
//			if (nCode < 0 || wParam != (IntPtr) KeyDownEventKey)
//				return CallNextHookEx(_hookId, nCode, wParam, lParam);

//			var vkCode = Marshal.ReadInt32(lParam);

//			Console.WriteLine((Keys) vkCode);

//			return CallNextHookEx(_hookId, nCode, wParam, lParam);
//		}

//		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

//		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//		[return: MarshalAs(UnmanagedType.Bool)]
//		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

//		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

//		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
//		private static extern IntPtr GetModuleHandle(string lpModuleName);
//	}
//}