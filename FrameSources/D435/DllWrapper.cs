using Common;
using System.Runtime.InteropServices;

namespace FrameSources.D435
{
	internal static class DllWrapper
	{
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ColorFrameCallback(ColorFrame* frame);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void DepthFrameCallback(DepthFrame* frame);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateFrameFeeder();

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void SubscribeToColorFrames(ColorFrameCallback progressCallback);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void UnsubscribeFromColorFrames(ColorFrameCallback progressCallback);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void SubscribeToDepthFrames(DepthFrameCallback progressCallback);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void UnsubscribeFromDepthFrames(DepthFrameCallback progressCallback);

		[DllImport("libFrameFeeder.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int DestroyFrameFeeder();
	}
}