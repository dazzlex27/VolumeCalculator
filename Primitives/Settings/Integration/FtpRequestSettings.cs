﻿namespace Primitives.Settings.Integration
{
	public class FtpRequestSettings
	{
		public bool EnableRequests { get; set; }

		public string Host { get; set; }

		public int Port { get; set; }

		public string Login { get; set; }

		public string Password { get; set; }

		public bool IsSecure { get; set; }

		public string BaseDirectory { get; set; }

		public bool IncludeObjectPhotos { get; set; }

		public FtpRequestSettings(bool enableRequests, string host, int port, string login, string password,
			bool isSecure, string baseDirectory, bool includeObjectPhotos)
		{
			EnableRequests = enableRequests;
			Host = host;
			Port = port;
			Login = login;
			Password = password;
			IsSecure = isSecure;
			BaseDirectory = baseDirectory;
			IncludeObjectPhotos = includeObjectPhotos;
		}

		public static FtpRequestSettings GetDefaultSettings()
		{
			return new FtpRequestSettings(false, "localhost", 21, "is", "", false, "is", false);
		}
	}
}