using System.Runtime.InteropServices;
using Microsoft.Kinect;
using Primitives;
using ColorFrame = Microsoft.Kinect.ColorFrame;
using DepthFrame = Microsoft.Kinect.DepthFrame;

namespace FrameProviders.Kinect
{
	internal static class KinectUtils
	{
		public static ImageData CreateColorFrameFromKinectFrame(ColorFrame colorFrame)
		{
			if (colorFrame == null)
				return null;

			var frameDescription = colorFrame.FrameDescription;
			var frameLength = frameDescription.Width * frameDescription.Height;
			var data = new byte[frameLength * 4];

			using (colorFrame.LockRawImageBuffer())
			{
				var pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
				var pointer = pinnedArray.AddrOfPinnedObject();

				colorFrame.CopyConvertedFrameDataToIntPtr(pointer, (uint)(frameLength * 4),
					ColorImageFormat.Bgra);

				pinnedArray.Free();
			}

			return new ImageData(frameDescription.Width, frameDescription.Height, data, 4);
		}

		public static DepthMap CreateDepthMapFromKinectFrame(DepthFrame depthFrame)
		{
			if (depthFrame == null)
				return null;

			var frameDescription = depthFrame.FrameDescription;
			var frameLength = frameDescription.Width * frameDescription.Height;
			var data = new short[frameLength];

			using (var depthBuffer = depthFrame.LockImageBuffer())
			{
				Marshal.Copy(depthBuffer.UnderlyingBuffer, data, 0, frameLength);
			}

			return new DepthMap(frameDescription.Width, frameDescription.Height, data);
		}
	}
}