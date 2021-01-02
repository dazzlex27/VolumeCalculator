using System;
using System.IO;
using System.Text;

namespace Primitives.Settings
{
    public class GeneralSettings
    {
        public string OutputPath { get; set; }

        public bool ShutDownPcByDefault { get; set; }

        public string ResultsFilePath => Path.Combine(OutputPath, GlobalConstants.ResultsFileName);

        public string PhotosDirectoryPath => Path.Combine(OutputPath, GlobalConstants.ResultPhotosFolder);

        public GeneralSettings(string outputPath, bool shutDownPcByDefault)
        {
            OutputPath = outputPath;
            ShutDownPcByDefault = shutDownPcByDefault;
        }
        
        public static GeneralSettings GetDefaultSettings()
        {
            var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var outputPath = Path.Combine(documentsFolder, "VolumeCalculationResults");

            return new GeneralSettings(outputPath, false);
        }

        public override string ToString()
        {
            var builder = new StringBuilder("GeneralSettings:");
            builder.Append($"OutputPath={OutputPath}");
            builder.Append($",ShutDownPcByDefault={ShutDownPcByDefault}");

            return builder.ToString();
        }
    }
}