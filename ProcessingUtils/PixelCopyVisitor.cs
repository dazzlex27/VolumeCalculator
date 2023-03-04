using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessingUtils
{
	internal class PixelCopyVisitor : IImageVisitor, IImageVisitorAsync
	{
		private byte[] _targetBytes;

		public PixelCopyVisitor(byte[] targetBytes)
		{
			_targetBytes = targetBytes;
		}

		public void Visit<TPixel>(Image<TPixel> image)
			where TPixel : unmanaged, IPixel<TPixel>
		{
			image.CopyPixelDataTo(_targetBytes);
		}

		public Task VisitAsync<TPixel>(Image<TPixel> image, CancellationToken cancellationToken)
			where TPixel : unmanaged, IPixel<TPixel>
		{
			throw new NotImplementedException();
		}
	}
}
