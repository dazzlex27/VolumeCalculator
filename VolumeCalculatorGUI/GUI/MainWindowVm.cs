using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Common;
using DepthMapProcessorGUI.Utils;

namespace DepthMapProcessorGUI.GUI
{
	internal class MainWindowVm : BaseViewModel
	{
		private bool _useColorStream;
		private bool _useDepthStream;
		private int _objWidth;
		private int _objHeight;
		private int _objDepth;
		private long _objVolume;
		private WriteableBitmap _colorImageBitmap;
		private WriteableBitmap _depthImageBitmap;

		public event Action<bool> UseColorStreamChanged;
		public event Action<bool> UseDepthStreamChanged;

		public bool UseColorStream
		{
			get => _useColorStream;
			set
			{
				if (_useColorStream == value)
					return;

				_useColorStream = value;
				OnPropertyChanged();
				UseColorStreamChanged?.Invoke(value);
			}
		}

		public bool UseDepthStream
		{
			get => _useDepthStream;
			set
			{
				if (_useDepthStream == value)
					return;

				_useDepthStream = value;
				OnPropertyChanged();
				UseDepthStreamChanged?.Invoke(value);
			}
		}

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

		public MainWindowVm()
		{
			_useColorStream = true;
			_useDepthStream = true;
		}

		public void UpdateColorImage(ImageData image)
		{
			var imageWidth = image.Width;
			var imageHeight = image.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			PixelFormat format = PixelFormats.BlackWhite;
			int bytesPerPixel = 0;
			switch (image.BytesPerPixel)
			{

				case 3:
					format = PixelFormats.Rgb24;
					bytesPerPixel = Constants.BytesPerPixel24;
					break;
				case 4:
					format = PixelFormats.Bgra32;
					bytesPerPixel = Constants.BytesPerPixel32;
					break;
			}
			var colorImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, format, null);
			colorImageBitmap.WritePixels(fullRect, image.Data, imageWidth * bytesPerPixel, 0);

			ColorImageBitmap = colorImageBitmap;
		}

		public void UpdateDepthImage(DepthMap depthMap, short minDepth, short distanceToFloor, short cutOffDepth)
		{
			var depthMapData = DepthMapUtils.GetColorizedDepthMapData(depthMap, minDepth, distanceToFloor, 
				cutOffDepth);

			var imageWidth = depthMap.Width;
			var imageHeight = depthMap.Height;
			var fullRect = new Int32Rect(0, 0, imageWidth, imageHeight);

			var depthImageBitmap = new WriteableBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Rgb24, null);
			depthImageBitmap.WritePixels(fullRect, depthMapData, imageWidth * Constants.BytesPerPixel24, 0);

			DepthImageBitmap = depthImageBitmap;
		}
	}
}