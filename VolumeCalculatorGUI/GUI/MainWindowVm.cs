using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DepthMapProcessorGUI.Entities;
using DepthMapProcessorGUI.Utils;

namespace DepthMapProcessorGUI.GUI
{
	internal class MainWindowVm : BaseViewModel
	{
		private int _objWidth;
		private int _objHeight;
		private int _objDepth;
		private long _objVolume;
		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		public int ObjWidth
		{
			get => _objWidth;
			set
			{
				if (_objWidth == value)
					return;

				_objWidth = value;
				OnPropertyChanged();
			}
		}

		public int ObjHeight
		{
			get => _objHeight;
			set
			{
				if (_objHeight == value)
					return;

				_objHeight = value;
				OnPropertyChanged();
			}
		}

		public int ObjDepth
		{
			get => _objDepth;
			set
			{
				if (_objDepth == value)
					return;

				_objDepth = value;
				OnPropertyChanged();
			}
		}

		public long ObjVolume
		{
			get => _objVolume;
			set
			{
				if (_objVolume == value)
					return;

				_objVolume = value;
				OnPropertyChanged();
			}
		}

		public WriteableBitmap ColorImageBitmap
		{
			get => _colorImageBitmap;
			set
			{
				if (Equals(_colorImageBitmap, value))
					return;

				_colorImageBitmap = value;
				OnPropertyChanged();
			}
		}

		public WriteableBitmap DepthImageBitmap
		{
			get => _depthImageBitmap;
			set
			{
				if (Equals(_depthImageBitmap, value))
					return;

				_depthImageBitmap = value;
				OnPropertyChanged();
			}
		}

		public void UpdateColorImage(ImageData image)
		{
			var imageWidth = image.Width;
			var imageHeight = image.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			var colorImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
			colorImageBitmap.WritePixels(fullRect, image.Data, imageWidth * Constants.BytesPerPixel24, 0);

			ColorImageBitmap = colorImageBitmap;
		}

		public void UpdateDepthImage(DepthMap depthMap, short distanceToFloor, short cutOffDepth)
		{
			var depthMapData = DepthMapUtils.GetColorizedDepthMapData(depthMap, Constants.MinDepth,
				distanceToFloor, cutOffDepth);

			var imageWidth = depthMap.Width;
			var imageHeight = depthMap.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			var depthImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
			depthImageBitmap.WritePixels(fullRect, depthMapData, imageWidth * Constants.BytesPerPixel24, 0);

			DepthImageBitmap = depthImageBitmap;
		}
	}
}