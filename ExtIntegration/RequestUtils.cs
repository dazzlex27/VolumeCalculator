using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Primitives;

namespace ExtIntegration
{
	public static class RequestUtils
	{
		public static string GenerateXmlResponseText(CalculationResultData resultData, bool includePhoto)
		{
			XElement content;

			var result = resultData.Result;
			var status = resultData.Status;

			switch (status)
			{
				case CalculationStatus.Sucessful:
					content = new XElement("calculationResult",
							new XElement("barcode", result.Barcode),
							new XElement("weight", $"{result.ObjectWeight}"),
							new XElement("length", result.ObjectLengthMm),
							new XElement("width", result.ObjectWidthMm),
							new XElement("height", result.ObjectHeightMm),
							new XElement("units", result.UnitCount),
							new XElement("comment", result.CalculationComment));

					if (includePhoto)
					{
						var photo = ImageUtils.GetBase64StringFromImageData(resultData.ObjectPhoto);
						content.Add(new XElement("photo", photo));
					}
					break;
				case CalculationStatus.Error:
					content = new XElement("calculationResult", "calculation error");
					break;
				case CalculationStatus.AbortedByUser:
					content = new XElement("calculationResult", "aborted");
					break;
				case CalculationStatus.Undefined:
					content = new XElement("calculationResult", "unknown error");
					break;
				case CalculationStatus.TimedOut:
					content = new XElement("calculationResult", "device connection error");
					break;
				case CalculationStatus.FailedToSelectAlgorithm:
					content = new XElement("calculationResult", "algorithm error");
					break;
				case CalculationStatus.ObjectNotFound:
					content = new XElement("calculationResult", "object not found");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}

			var doc = new XDocument(new XDeclaration("1.0", "us-utf8", null), content);

			using (var writer = new StringWriter())
			{
				doc.Save(writer);
				return writer.ToString();
			}
		}
	}
}