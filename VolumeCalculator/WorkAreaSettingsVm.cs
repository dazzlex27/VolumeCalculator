using GuiCommon;
using Primitives.Settings;

namespace VolumeCalculator
{
	internal class WorkAreaSettingsVm : BaseViewModel
	{
		private short _floorDepth;
		private short _minObjHeight;
		private bool _useColorMask;
		private MaskPolygonControlVm _colorMaskRectangleControlVm;
		private bool _useDepthMask;
		private MaskPolygonControlVm _depthMaskPolygonControlVm;

		public short FloorDepth
		{
			get => _floorDepth;
			set => SetField(ref _floorDepth, value, nameof(FloorDepth));
		}

		public short MinObjHeight
		{
			get => _minObjHeight;
			set => SetField(ref _minObjHeight, value, nameof(MinObjHeight));
		}

		public bool UseColorMask
		{
			get => _useColorMask;
			set => SetField(ref _useColorMask, value, nameof(UseColorMask));
		}

		public MaskPolygonControlVm ColorMaskRectangleControlVm
		{
			get => _colorMaskRectangleControlVm;
			set => SetField(ref _colorMaskRectangleControlVm, value, nameof(ColorMaskRectangleControlVm));
		}

		public bool UseDepthMask
		{
			get => _useDepthMask;
			set => SetField(ref _useDepthMask, value, nameof(UseDepthMask));
		}

		public MaskPolygonControlVm DepthMaskPolygonControlVm
		{
			get => _depthMaskPolygonControlVm;
			set => SetField(ref _depthMaskPolygonControlVm, value, nameof(DepthMaskPolygonControlVm));
		}

		public WorkAreaSettingsVm(WorkAreaSettings settings)
		{
			FloorDepth = settings.FloorDepth;
			MinObjHeight = settings.MinObjectHeight;
			UseColorMask = settings.UseColorMask;
			ColorMaskRectangleControlVm = new MaskPolygonControlVm(settings.ColorMaskContour);
			UseDepthMask = settings.UseDepthMask;
			DepthMaskPolygonControlVm = new MaskPolygonControlVm(settings.DepthMaskContour);
		}

		public WorkAreaSettings GetSettings()
		{
			var colorMaskPoints = ColorMaskRectangleControlVm.GetPolygonPoints();
			var depthMaskPoints = DepthMaskPolygonControlVm.GetPolygonPoints();

			return new WorkAreaSettings(FloorDepth, MinObjHeight, UseColorMask, colorMaskPoints, UseDepthMask, depthMaskPoints);
		}
	}
}