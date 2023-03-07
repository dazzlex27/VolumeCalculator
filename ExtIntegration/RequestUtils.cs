using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Primitives.Calculation;
using ProcessingUtils;

namespace ExtIntegration
{
	public static class RequestUtils
	{
		public static async Task<string> GenerateXmlResponseTextAsync(CalculationResultData resultData, bool includePhoto)
		{
			XElement content;

			var result = resultData.Result;
			var status = resultData.Status;

			switch (status)
			{
				case CalculationStatus.Successful:
					content = new XElement("calculationResult",
							new XElement("barcode", result.Barcode),
							new XElement("weight", $"{result.ObjectWeight}"),
							new XElement("length", result.ObjectLengthMm),
							new XElement("width", result.ObjectWidthMm),
							new XElement("height", result.ObjectHeightMm),
							new XElement("unitCount", result.UnitCount),
							new XElement("comment", result.CalculationComment));

					if (includePhoto)
					{
						var photo = await ImageUtils.GetBase64StringFromImageDataAsync(resultData.ObjectPhoto);
						content.Add(new XElement("photo", photo));
					}
					break;
				case CalculationStatus.CalculationError:
					content = new XElement("calculationResult", "Failed to calculate volume");
					break;
				case CalculationStatus.AbortedByUser:
					content = new XElement("calculationResult", "Aborted by user");
					break;
				case CalculationStatus.Ready:
					content = new XElement("calculationResult", "Unknown error");
					break;
				case CalculationStatus.TimedOut:
					content = new XElement("calculationResult", "Device connection error");
					break;
				case CalculationStatus.FailedToSelectAlgorithm:
					content = new XElement("calculationResult", "Algorithm error");
					break;
				case CalculationStatus.ObjectNotFound:
					content = new XElement("calculationResult", "Object not found");
					break;
				case CalculationStatus.BarcodeNotEntered:
					content = new XElement("calculationResult", "Barcode not entered");
					break;
				case CalculationStatus.Pending:
					content = new XElement("calculationResult", "Calculation is pending");
					break;
				case CalculationStatus.FailedToStart:
					content = new XElement("calculationResult", "Failed to start calculation");
					break;
				case CalculationStatus.WeightNotStable:
					content = new XElement("calculationResult", "Weight was not stable");
					break;
				case CalculationStatus.InProgress:
					content = new XElement("calculationResult", "Calculation in progress");
					break;
				case CalculationStatus.FailedToCloseFiles:
					content = new XElement("calculationResult", "Failed to close open result files");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}

			var doc = new XDocument(new XDeclaration("1.0", "us-utf8", null), content);
			using var writer = new StringWriter();
			doc.Save(writer);
			return writer.ToString();
		}
	}
}
