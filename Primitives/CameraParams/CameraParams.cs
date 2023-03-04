namespace Primitives.CameraParams
{
	public abstract class CameraParams
	{
		public float FovX { get; }

		public float FovY { get; }

		public float FocalLengthX { get; }

		public float FocalLengthY { get; }

		public float PrincipalPointX { get; }

		public float PrincipalPointY { get; }

		protected CameraParams(float fovX, float fovY, float focalLengthX, float focalLengthY, float principalPointX, float principalPointY)
		{
			FovX = fovX;
			FovY = fovY;
			FocalLengthX = focalLengthX;
			FocalLengthY = focalLengthY;
			PrincipalPointX = principalPointX;
			PrincipalPointY = principalPointY;
		}
	}
}