using System.Runtime.InteropServices;
using Common;

namespace FrameProviders.D435
{
	internal static class DllWrapper
	{
		private const string LibName = "libD435FrameProvider.dll";

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void ColorFrameCallback(ColorFrame* frame);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void DepthFrameCallback(DepthFrame* frame);

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int CreateFrameProvider();

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern DepthCameraIntrinsics GetDepthCameraIntrinsics();

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SubscribeToColorFrames(ColorFrameCallback progressCallback);

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void UnsubscribeFromColorFrames(ColorFrameCallback progressCallback);

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SubscribeToDepthFrames(DepthFrameCallback progressCallback);

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern void UnsubscribeFromDepthFrames(DepthFrameCallback progressCallback);

		[DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
		public static extern int DestroyFrameProvider();
	}
}