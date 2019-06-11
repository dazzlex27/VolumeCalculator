namespace ComTestApp
{
	public interface IRangeMeter
	{
		int GetReading();

		void ToggleLaser(bool enable);
	}
}