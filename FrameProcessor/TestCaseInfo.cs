﻿namespace FrameProcessor
{
	public class TestCaseInfo
	{
		public string Casename { get; }

		public string Description { get; }

		public string SavingDirectory { get; }

		public int ObjLength { get; }

		public int ObjWidth { get; }

		public int ObjHeight { get; }

		public int TimesToSave { get; }

		public TestCaseInfo(string casename, string description, string savingDirectory, int objLength,
			int objWidth,
			int objHeight, int timesToSave)
		{
			Casename = casename;
			Description = description;
			SavingDirectory = savingDirectory;
			ObjWidth = objWidth;
			ObjHeight = objHeight;
			ObjLength = objLength;
			TimesToSave = timesToSave;
		}
	}
}